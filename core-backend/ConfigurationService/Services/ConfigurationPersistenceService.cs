using FixBackendShared.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TradeData;
using TradeData.Entities;
using EfConfigurationEntity = TradeData.Entities.ConfigurationEntity;
using ProtoConfigurationEntity = FixBackendShared.Grpc.ConfigurationEntity;

namespace ConfigurationService.Services;

public class ConfigurationPersistenceService : ConfigurationPersistance.ConfigurationPersistanceBase
{
	private readonly TradeDbContext dbContext;
	private readonly ILogger<ConfigurationPersistenceService> logger;

	public ConfigurationPersistenceService(TradeDbContext dbContext, ILogger<ConfigurationPersistenceService> logger)
	{
		this.dbContext = dbContext;
		this.logger = logger;
	}

	private static readonly IReadOnlyDictionary<ConfigurationEntityType, Func<EfConfigurationEntity>> Factories =
		new Dictionary<ConfigurationEntityType, Func<EfConfigurationEntity>>
	{
		[ConfigurationEntityType.Strategy] = () => new StrategyEntity(),
		[ConfigurationEntityType.Broker]   = () => new BrokerEntity(),
		[ConfigurationEntityType.Fund]     = () => new FundEntity(),
		[ConfigurationEntityType.Exchange] = () => new ExchangeEntity(),
		[ConfigurationEntityType.Manager]  = () => new ManagerEntity(),
	};

	private static readonly IReadOnlyDictionary<ConfigurationEntityType, Func<TradeDbContext, IQueryable<EfConfigurationEntity>>> Queries =
		new Dictionary<ConfigurationEntityType, Func<TradeDbContext, IQueryable<EfConfigurationEntity>>>
	{
		[ConfigurationEntityType.Strategy] = ctx => ctx.Strategies,
		[ConfigurationEntityType.Broker]   = ctx => ctx.Brokers,
		[ConfigurationEntityType.Fund]     = ctx => ctx.Funds,
		[ConfigurationEntityType.Exchange] = ctx => ctx.Exchanges,
		[ConfigurationEntityType.Manager]  = ctx => ctx.Managers,
	};

	public override async Task<PersistResponse> Persist(ProtoConfigurationEntity request, ServerCallContext context)
	{
		var factory = Factory(request.Type);
		using var _ = logger.BeginScope(new Dictionary<string, object> { [request.Type.ToString()] = request.Code });

		var entity = factory();
		entity.Code = request.Code;
		entity.Description = request.Description;

		dbContext.Add(entity);
		await dbContext.SaveChangesAsync(context.CancellationToken);

		logger.LogInformation("Persisted {Type} entity {Id}", request.Type, entity.Id);
		return new PersistResponse { Id = entity.Id };
	}

	public override async Task<GetAllResponse> GetAll(GetAllRequest request, ServerCallContext context)
	{
		var types = request.TypeFilter.Count > 0 ? request.TypeFilter.Distinct() : Queries.Keys;

		var response = new GetAllResponse();
		foreach (var type in types)
		{
			var rows = await Query(type)(dbContext)
				.AsNoTracking()
				.OrderBy(x => x.Id)
				.ToListAsync(context.CancellationToken);

			response.Entities.AddRange(rows.Select(e => MapToProto(e, type)));
		}

		return response;
	}

	public override async Task<ProtoConfigurationEntity> GetById(GetByIdRequest request, ServerCallContext context)
	{
		var entity = await Query(request.Type)(dbContext)
			.AsNoTracking()
			.FirstOrDefaultAsync(e => e.Id == request.Id, context.CancellationToken);

		if (entity is null)
		{
			logger.LogWarning("{Type} entity {Id} not found", request.Type, request.Id);
			throw new RpcException(new Status(StatusCode.NotFound, $"{request.Type} '{request.Id}' not found"));
		}

		return MapToProto(entity, request.Type);
	}

	private static Func<EfConfigurationEntity> Factory(ConfigurationEntityType type) =>
		Factories.TryGetValue(type, out var f)
			? f
			: throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unsupported configuration entity type '{type}'"));

	private static Func<TradeDbContext, IQueryable<EfConfigurationEntity>> Query(ConfigurationEntityType type) =>
		Queries.TryGetValue(type, out var q)
			? q
			: throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unsupported configuration entity type '{type}'"));

	private static ProtoConfigurationEntity MapToProto(EfConfigurationEntity e, ConfigurationEntityType type) =>
		new()
		{
			Id = e.Id,
			Code = e.Code,
			Description = e.Description,
			Type = type
		};
}