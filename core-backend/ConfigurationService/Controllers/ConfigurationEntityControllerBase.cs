using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeData;
using TradeData.Entities;

namespace ConfigurationService.Controllers;

public abstract class ConfigurationEntityControllerBase<TEntity> : ControllerBase
	where TEntity : ConfigurationEntity, new()
{
	protected ILogger<ConfigurationEntityControllerBase<TEntity>> logger;
	protected TradeDbContext dbContext;

	public ConfigurationEntityControllerBase(TradeDbContext dbContext, ILogger<ConfigurationEntityControllerBase<TEntity>> logger)
	{
		this.dbContext = dbContext;
		this.logger = logger;
	}

	[HttpGet]
	public Task<IActionResult> GetAll(CancellationToken cancellationToken)
		=> this.GetAllEntities(cancellationToken);

	[HttpGet("{id:int}")]
	public Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
		=> this.GetEntityById(id, cancellationToken);

	[HttpPost("create")]
	public Task<IActionResult> Create([FromBody]CreateEntityRequest request, CancellationToken cancellationToken)
		=> this.CreateEntity(request, cancellationToken);

	private async Task<IActionResult> CreateEntity(CreateEntityRequest request, CancellationToken cancellationToken)
	{
		var name = GetEntityName();
		using var _ = logger.BeginScope(new Dictionary<string, object> { [name] = request.Code });

		TEntity entity = new TEntity()
		{
			Code = request.Code,
			Description = request.Description
		};

		dbContext
			.Set<TEntity>()
			.Add(entity);

		var success = await PersistEntity(entity, cancellationToken);

		return success ? Ok() : StatusCode(500);
	}

	protected async Task<IActionResult> GetAllEntities(CancellationToken cancellationToken)
	{
		var name = GetEntityName();
		using var _ = logger.BeginScope(new Dictionary<string, object> { [name] = "Fetching all entities." });

		IList<TEntity> entities;
		try
		{
			entities = await dbContext.Set<TEntity>().ToListAsync(cancellationToken);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Error fetching entities.");
			return StatusCode(500);
		}

		return Ok(entities);
	}

	protected async Task<IActionResult> GetEntityById(int id, CancellationToken cancellationToken)
	{
		var name = GetEntityName();
		using var _ = logger.BeginScope(new Dictionary<string, object> { [name] = id });

		TEntity? entity;
		try
		{
			entity = await dbContext.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Error fetching entity id {id}", id);
			return StatusCode(500);
		}

		if (entity == null)
		{
			logger.LogWarning("Warning entity id {id} not found", id);
			return StatusCode(404);
		}

		return Ok(entity);
	}

	private static string GetEntityName()
	{
		return typeof(TEntity).Name.Replace("Entity", "");
	}

	private async Task<bool> PersistEntity(ConfigurationEntity entity, CancellationToken cancellationToken)
	{
		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
			logger.LogInformation("Persisted entity: {entity}", entity.ToString());
		}
		catch (Exception e)
		{
			logger.LogError(e, "Error persisting entity: {entity}", entity.ToString());
			return false;
		}

		return true;
	}
}