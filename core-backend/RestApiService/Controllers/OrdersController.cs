using System.Globalization;
using Shared.Grpc;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using RestApiService.Models;

namespace RestApiService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
	private readonly OrderPersistence.OrderPersistenceClient orderClient;
	private readonly ILogger<OrdersController> logger;

	public OrdersController(OrderPersistence.OrderPersistenceClient orderClient, ILogger<OrdersController> logger)
	{
		this.orderClient = orderClient;
		this.logger = logger;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
	{
		using var _ = logger.BeginScope(new Dictionary<string, object> { ["Order"] = "Fetching all orders." });

		try
		{
			var response = await orderClient.GetAllOrdersAsync(new GetAllOrdersRequest(), cancellationToken: cancellationToken);
			return Ok(response.Orders.Select(MapToResponse));
		}
		catch (RpcException rpe)
		{
			logger.LogError(rpe, "Error fetching orders.");
			return StatusCode(500);
		}
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
	{
		using var _ = logger.BeginScope(new Dictionary<string, object> { ["Order"] = id });

		try
		{
			var order = await orderClient.GetOrderByIdAsync(new GetOrderByIdRequest{ Id = id }, cancellationToken: cancellationToken);
			return Ok(MapToResponse(order));
		}
		catch (RpcException rpe) when (rpe.StatusCode == Grpc.Core.StatusCode.NotFound)
		{
			logger.LogWarning("Warning order id {id} not found", id);
			return NotFound();
		}
		catch (RpcException rpe)
		{
			logger.LogError(rpe, "Error fetching order id {id}", id);
			return StatusCode(500);
		}
	}

	private static OrderResponse MapToResponse(Order order) => new(
		order.Id,
		order.OrderId,
		order.Symbol,
		order.Side.ToString(),
		decimal.Parse(order.Quantity, CultureInfo.InvariantCulture),
		decimal.Parse(order.Price, CultureInfo.InvariantCulture)
	);
}