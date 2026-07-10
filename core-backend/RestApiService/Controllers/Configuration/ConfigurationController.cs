using Shared.Grpc;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using RestApiService.Models;

namespace RestApiService.Controllers.Configuration;

[Route("api/configuration")]
[ApiController]
public class ConfigurationController : ControllerBase
{
	private readonly ConfigurationPersistance.ConfigurationPersistanceClient client;
	private readonly ILogger<ConfigurationController> logger;

	public ConfigurationController(ConfigurationPersistance.ConfigurationPersistanceClient client, ILogger<ConfigurationController> logger)
	{
		this.client = client;
		this.logger = logger;
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] CreateEntityRequest request, CancellationToken cancellationToken)
	{
		if (request.Type == ConfigurationEntityType.Unspecified)
			return BadRequest("Configuration entity type is required.");

		using var _ = logger.BeginScope(new Dictionary<string, object> { [request.Type.ToString()] = request.Code });

		try
		{
			var response = await client.PersistAsync(new ConfigurationEntity
			{
				Code = request.Code,
				Description = request.Description,
				Type = request.Type
			}, cancellationToken: cancellationToken);

			logger.LogInformation("Persisted {Type} entity {Id}", request.Type, response.Id);
			return Ok(response.Id);
		}
		catch (RpcException rpc) when (rpc.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
		{
			logger.LogWarning("Create rejected: {Detail}", rpc.Status.Detail);
			return BadRequest(rpc.Status.Detail);
		}
		catch (RpcException rpe)
		{
			logger.LogError(rpe, "Error persisting entity.");
			return StatusCode(500);
		}
	}

	[HttpGet]
	public async Task<IActionResult> GetAll([FromQuery] ConfigurationEntityType[] types, CancellationToken cancellationToken)
	{
		var request = new GetAllRequest();
		request.TypeFilter.AddRange(types);

		try
		{
			var response = await client.GetAllAsync(request, cancellationToken: cancellationToken);
			return Ok(response.Entities.Select(MapToResponse));
		}
		catch (RpcException rpe) when (rpe.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
		{
			logger.LogWarning("Invalid type filter: {Detail}", rpe.Status.Detail);
			return BadRequest(rpe.Status.Detail);
		}
		catch (RpcException rpe)
		{
			logger.LogError(rpe, "Error fetching configuration entities.");
			return StatusCode(500);
		}
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] ConfigurationEntityType type, CancellationToken cancellationToken)
	{
		if (type == ConfigurationEntityType.Unspecified)
			return BadRequest("Configuration entity type is required.");

		using var _ = logger.BeginScope(new Dictionary<string, object> { [type.ToString()] = id });

		try
		{
			var entity = await client.GetByIdAsync(new GetByIdRequest { Id = id, Type = type }, cancellationToken: cancellationToken);
			return Ok(MapToResponse(entity));
		}
		catch (RpcException rpe) when (rpe.StatusCode == Grpc.Core.StatusCode.NotFound)
		{
			logger.LogWarning("Configuration entity id {id} not found", id);
			return NotFound();
		}
		catch (RpcException rpe)
		{
			logger.LogError(rpe, "Error fetching configuration entity id {id}", id);
			return StatusCode(500);
		}
	}

	private static ConfigurationEntityResponse MapToResponse(ConfigurationEntity entity) =>
		new(entity.Id, entity.Code, entity.Description, entity.Type);
}