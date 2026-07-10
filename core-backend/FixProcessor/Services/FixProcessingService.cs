using Shared.Grpc;
using FixProcessor.Parser;
using Grpc.Core;

namespace FixProcessor.Services;

public class FixProcessingService : FixProcessing.FixProcessingBase
{
	private readonly OrderPersistence.OrderPersistenceClient orderClient;
	private readonly ILogger<FixProcessingService> logger;

	public FixProcessingService(OrderPersistence.OrderPersistenceClient orderClient, ILogger<FixProcessingService> logger)
	{
		this.logger = logger;
		this.orderClient = orderClient;
	}

	public override async Task<FixProcessResponse> Process(FixProcessRequest request, ServerCallContext context)
	{
		var cancellationToken = context.CancellationToken;
		using var _ = logger.BeginScope(new Dictionary<string, object> { ["fixId"] = request.Id });

		PersistOrderRequest orderRequest;

		try
		{
			orderRequest = FixParser.ToPersistRequest(request);
		}
		catch (FormatException fe)
		{
			logger.LogWarning(fe, "Unparseable FIX {Id}", request.Id);
			throw new RpcException(new Status(StatusCode.InvalidArgument, fe.Message));
		}

		try
		{
			var response = await orderClient.PersistAsync(orderRequest, cancellationToken: cancellationToken);
			logger.LogInformation(
				response.AlreadyExisted
					? "Order {OrderId} already persisted, treating as success"
					: "Persisted order {OrderId}",
				orderRequest.OrderId);

			return new FixProcessResponse { AlreadyExisted = response.AlreadyExisted };
		}
		catch (RpcException rpc) when (rpc.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument)
		{
			logger.LogWarning("Order {OrderId} rejected: {Detail}", orderRequest.OrderId, rpc.Status.Detail);
			throw new RpcException(new Status(StatusCode.InvalidArgument, rpc.Status.Detail));
		}
	}
}