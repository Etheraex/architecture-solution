using Grpc.Core;
using Shared.Grpc;
using TradeData;
using TradeData.Entities;
using EfConfigurationEntity = TradeData.Entities.ConfigurationEntity;
using ProtoConfigurationEntity = Shared.Grpc.ConfigurationEntity;

namespace ConfigurationService.Catalog;

public static class ConfigurationCatalog
{
	public static readonly IReadOnlyDictionary<ConfigurationEntityType, Func<EfConfigurationEntity>> Factories =
		new Dictionary<ConfigurationEntityType, Func<EfConfigurationEntity>>
	{
		[ConfigurationEntityType.Strategy] = () => new StrategyEntity(),
		[ConfigurationEntityType.Broker]   = () => new BrokerEntity(),
		[ConfigurationEntityType.Fund]     = () => new FundEntity(),
		[ConfigurationEntityType.Exchange] = () => new ExchangeEntity(),
		[ConfigurationEntityType.Manager]  = () => new ManagerEntity(),
	};

	public static readonly IReadOnlyDictionary<ConfigurationEntityType, Func<TradeDbContext, IQueryable<EfConfigurationEntity>>> Queries =
		new Dictionary<ConfigurationEntityType, Func<TradeDbContext, IQueryable<EfConfigurationEntity>>>
	{
		[ConfigurationEntityType.Strategy] = ctx => ctx.Strategies,
		[ConfigurationEntityType.Broker]   = ctx => ctx.Brokers,
		[ConfigurationEntityType.Fund]     = ctx => ctx.Funds,
		[ConfigurationEntityType.Exchange] = ctx => ctx.Exchanges,
		[ConfigurationEntityType.Manager]  = ctx => ctx.Managers,
	};

	public static IEnumerable<ConfigurationEntityType> Types => Queries.Keys;

	public static Func<EfConfigurationEntity> Factory(ConfigurationEntityType type) =>
		Factories.TryGetValue(type, out var f)
			? f
			: throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unsupported configuration entity type '{type}'"));

	public static Func<TradeDbContext, IQueryable<EfConfigurationEntity>> Query(ConfigurationEntityType type) =>
		Queries.TryGetValue(type, out var q)
			? q
			: throw new RpcException(new Status(StatusCode.InvalidArgument, $"Unsupported configuration entity type '{type}'"));

	public static ProtoConfigurationEntity MapToProto(EfConfigurationEntity e, ConfigurationEntityType type) =>
		new()
		{
			Id = e.Id,
			Code = e.Code,
			Description = e.Description,
			Type = type
		};
}