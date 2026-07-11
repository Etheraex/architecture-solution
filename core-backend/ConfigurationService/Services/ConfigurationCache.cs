using System.Collections.Concurrent;
using Google.Protobuf;
using Shared.Grpc;
using StackExchange.Redis;
using ProtoConfigurationEntity = Shared.Grpc.ConfigurationEntity;

namespace ConfigurationService.Services;

public sealed class ConfigurationCache
{
	private readonly IConnectionMultiplexer redis;
	private readonly ILogger<ConfigurationCache> logger;
	private readonly ConcurrentDictionary<ConfigurationEntityType, bool> complete = new();

	private IDatabase Db => redis.GetDatabase();

	public ConfigurationCache(IConnectionMultiplexer redis, ILogger<ConfigurationCache> logger)
	{
		this.redis = redis;
		this.logger = logger;

		// Redis failed and comes back empty
		// Need to reset completeness bits and let GetAll re-warm from DB
		redis.ConnectionFailed += (_, _) => complete.Clear();
	}

	public bool IsComplete(ConfigurationEntityType type) => complete.TryGetValue(type, out var value) && value;

	private static string KeyFor(ConfigurationEntityType type) => $"config:{type}";

	public async Task<ProtoConfigurationEntity?> TryGetByIdAsync(ConfigurationEntityType type, int id)
	{
		if (!redis.IsConnected)
			return null;

		try
		{
			var value = await Db.HashGetAsync(KeyFor(type), id);
			return value.IsNullOrEmpty ? null : ProtoConfigurationEntity.Parser.ParseFrom(value);
		}
		catch (RedisException re)
		{
			logger.LogWarning(re, "Cache read failed for {Type} {Id}", type, id);
			return null;
		}
	}

	public async Task<IReadOnlyList<ProtoConfigurationEntity>?> TryGetAllAsync(ConfigurationEntityType type)
	{
		if (!IsComplete(type) || !redis.IsConnected)
			return null;

		try
		{
			var entries = await Db.HashGetAllAsync(KeyFor(type));
			return entries.Select(e => ProtoConfigurationEntity.Parser.ParseFrom(e.Value)).ToList();
		}
		catch (RedisException re)
		{
			logger.LogWarning(re, "Cache read-all failed for {Type}", type);
			return null;
		}
	}

	public async Task SetAsync(ProtoConfigurationEntity entity)
	{
		if (!redis.IsConnected)
		{
			complete[entity.Type] = false;
			return;
		}

		try
		{
			await Db.HashSetAsync(KeyFor(entity.Type), entity.Id, entity.ToByteArray());
		}
		catch (RedisException re)
		{
			complete[entity.Type] = false;
			logger.LogWarning(re, "Cache write failed for {Type} {Id}; marking type incomplete", entity.Type, entity.Id);
		}
	}

	public async Task RepopulateAsync(ConfigurationEntityType type, IReadOnlyCollection<ProtoConfigurationEntity> entities)
	{
		if (!redis.IsConnected)
		{
			complete[type] = false;
			return;
		}

		try
		{
			var fields = entities
				.Select(e => new HashEntry(e.Id, e.ToByteArray()))
				.ToArray();

			if (fields.Length > 0)
				await Db.HashSetAsync(KeyFor(type), fields);

			complete[type] = true;
		}
		catch (RedisException re)
		{
			complete[type] = false;
			logger.LogWarning(re, "Cache repopulate failed for {Type}; leaving incomplete", type);
		}
	}
}