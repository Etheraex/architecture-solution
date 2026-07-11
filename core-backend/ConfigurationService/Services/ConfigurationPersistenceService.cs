using Shared.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TradeData;
using ProtoConfigurationEntity = Shared.Grpc.ConfigurationEntity;
using ConfigurationService.Catalog;

namespace ConfigurationService.Services;

public class ConfigurationPersistenceService : ConfigurationPersistance.ConfigurationPersistanceBase
{
	private readonly TradeDbContext dbContext;
	private readonly ConfigurationCache cache;
	private readonly ILogger<ConfigurationPersistenceService> logger;

	public ConfigurationPersistenceService(TradeDbContext dbContext, ConfigurationCache cache, ILogger<ConfigurationPersistenceService> logger)
	{
		this.dbContext = dbContext;
		this.cache = cache;
		this.logger = logger;
	}

	public override async Task<PersistResponse> Persist(ProtoConfigurationEntity request, ServerCallContext context)
	{
		var factory = ConfigurationCatalog.Factory(request.Type);
		using var _ = logger.BeginScope(new Dictionary<string, object> { [request.Type.ToString()] = request.Code });

		var entity = factory();
		entity.Code = request.Code;
		entity.Description = request.Description;

		dbContext.Add(entity);
		await dbContext.SaveChangesAsync(context.CancellationToken);

		await cache.SetAsync(ConfigurationCatalog.MapToProto(entity, request.Type));

		logger.LogInformation("Persisted {Type} entity {Id}", request.Type, entity.Id);
		return new PersistResponse { Id = entity.Id };
	}

	public override async Task<GetAllResponse> GetAll(GetAllRequest request, ServerCallContext context)
	{
		var types = request.TypeFilter.Count > 0 ? request.TypeFilter.Distinct() : ConfigurationCatalog.Queries.Keys;

		var response = new GetAllResponse();
		foreach (var type in types)
			response.Entities.AddRange(await GetAllOfType(type, context.CancellationToken));

		return response;
	}

	private async Task<IReadOnlyCollection<ProtoConfigurationEntity>> GetAllOfType(ConfigurationEntityType type, CancellationToken cancellationToken)
	{
		var cached = await cache.TryGetAllAsync(type);

		if (cached is not null)
			return cached;

		var rows = await ConfigurationCatalog.Query(type)(dbContext)
			.AsNoTracking()
			.OrderBy(x => x.Id)
			.ToListAsync(cancellationToken);

		var entities = rows.Select(e => ConfigurationCatalog.MapToProto(e, type)).ToList();
		await cache.RepopulateAsync(type, entities);
		return entities;
	}

	public override async Task<ProtoConfigurationEntity> GetById(GetByIdRequest request, ServerCallContext context)
	{
		var cached = await cache.TryGetByIdAsync(request.Type, request.Id);
		if (cached != null)
			return cached;

		var entity = await ConfigurationCatalog.Query(request.Type)(dbContext)
			.AsNoTracking()
			.FirstOrDefaultAsync(e => e.Id == request.Id, context.CancellationToken);

		if (entity is null)
		{
			logger.LogWarning("{Type} entity {Id} not found", request.Type, request.Id);
			throw new RpcException(new Status(StatusCode.NotFound, $"{request.Type} '{request.Id}' not found"));
		}

		// Backfill cache miss
		var proto = ConfigurationCatalog.MapToProto(entity, request.Type);
		await cache.SetAsync(proto);

		return proto;
	}
}