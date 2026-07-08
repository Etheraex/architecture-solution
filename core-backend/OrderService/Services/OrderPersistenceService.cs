using System.Globalization;
using FixBackendShared.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TradeData;
using TradeData.Entities;
using ProtoOrder = FixBackendShared.Grpc.Order;

namespace OrderService.Services;

public class OrderPersistenceService : OrderPersistence.OrderPersistenceBase
{
	private const int NoneId = 1;

	private readonly TradeDbContext dbContext;
	private readonly ILogger<OrderPersistenceService> logger;

	public OrderPersistenceService(TradeDbContext dbContext, ILogger<OrderPersistenceService> logger)
	{
		this.dbContext = dbContext;
		this.logger = logger;
	}

	public override async Task<PersistOrderResponse> Persist(PersistOrderRequest request, ServerCallContext context)
	{
		var cancellationToken = context.CancellationToken;
		using var _ = logger.BeginScope(new Dictionary<string, object> { ["Order Id"] = request.OrderId });

		var security = await dbContext.Set<Security>()
			.FirstOrDefaultAsync(x => x.Ticker == request.Symbol, cancellationToken);

		if (security is null)
		{
			logger.LogWarning("Unknown security ticker {Ticker}", request.Symbol);
			throw new RpcException(new Status(StatusCode.NotFound, $"Unknown security ticker '{request.Symbol}'"));
		}

		var order = new TradeData.Entities.Order
		{
			OrderId = request.OrderId,
			SecurityId = security.Id,
			Side = MapSide(request.Side),
			Quantity = ParseDecimal(request.Quantity, "quantity"),
			Price = ParseDecimal(request.Price, "price"),
			StrategyId = NoneId,
			FundId = NoneId,
			BrokerId = NoneId,
			ManagerId = NoneId,
		};

		dbContext.Orders.Add(order);

		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
			logger.LogInformation("Persisted order {OrderId}", order.OrderId);
			return new PersistOrderResponse { AlreadyExisted = false };
		}
		catch (DbUpdateException dbe) when (IsUniqueViolation(dbe))
		{
			logger.LogInformation("Order {OrderId} already persisted, treating as success", order.OrderId);
			return new PersistOrderResponse { AlreadyExisted = true };
		}
	}

	public override async Task<GetAllOrdersResponse> GetAllOrders(GetAllOrdersRequest request, ServerCallContext context)
	{
		var orders = await dbContext.Orders
			.Include(o => o.Security)
			.AsNoTracking()
			.OrderBy(o => o.Id)
			.ToListAsync(context.CancellationToken);
		
		var response = new GetAllOrdersResponse();
		response.Orders.AddRange(orders.Select(MapToProto));

		return response;
	}

	public override async Task<ProtoOrder> GetOrderById(GetOrderByIdRequest request, ServerCallContext context)
	{
		var order = await dbContext.Orders
			.Include(o => o.Security)
			.AsNoTracking()
			.FirstOrDefaultAsync(o => o.Id == request.Id, context.CancellationToken);

		if (order is null)
		{
			logger.LogWarning("Order {Id} not found", request.Id);
			throw new RpcException(new Status(StatusCode.NotFound, $"Order '{request.Id}' not found"));
		}

		return MapToProto(order);
	}

	private static ProtoOrder MapToProto(TradeData.Entities.Order order) => new()
	{
		Id       = order.Id,
		OrderId  = order.OrderId,
		Symbol   = order.Security.Ticker,
		Side     = ToProtoSide(order.Side),
		Quantity = order.Quantity.ToString(CultureInfo.InvariantCulture),
		Price    = order.Price.ToString(CultureInfo.InvariantCulture),
	};

	private static Side ToProtoSide(OrderSide side) => side switch
	{
		OrderSide.Buy  => Side.Buy,
		OrderSide.Sell => Side.Sell,
		_              => Side.None,
	};

	private static OrderSide MapSide(Side side) => side switch
	{
		Side.Buy => OrderSide.Buy,
		Side.Sell => OrderSide.Sell,
		_ => OrderSide.None
	};

	private static decimal ParseDecimal(string value, string field)
		=> decimal.TryParse(value, System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
			? d
			: throw new RpcException(new Status(StatusCode.InvalidArgument, $"{field} is not a number: '{value}'"));

	// 2601 = duplicate key in a unique index, 2627 = unique constraint violation.
	private static bool IsUniqueViolation(DbUpdateException dbe)
		=> dbe.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };
}