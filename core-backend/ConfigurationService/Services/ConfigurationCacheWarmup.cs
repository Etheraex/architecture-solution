using TradeData;
using ConfigurationService.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationService.Services;

public class ConfigurationCacheWarmup : BackgroundService
{
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ConfigurationCache cache;
	private readonly ILogger<ConfigurationCacheWarmup> logger;

	public ConfigurationCacheWarmup(IServiceScopeFactory scopeFactory, ConfigurationCache cache, ILogger<ConfigurationCacheWarmup> logger)
	{
		this.scopeFactory = scopeFactory;
		this.cache = cache;
		this.logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			using var scope = scopeFactory.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<TradeDbContext>();

			foreach (var type in ConfigurationCatalog.Types)
			{
				var rows = await ConfigurationCatalog.Query(type)(dbContext)
					.AsNoTracking()
					.OrderBy(x => x.Id)
					.ToListAsync(stoppingToken);

				var entities = rows.Select(e => ConfigurationCatalog.MapToProto(e, type)).ToList();
				await cache.RepopulateAsync(type, entities);

				logger.LogInformation("Warmed {Count} {Type} entities into cache", entities.Count, type);
			}
		}
		catch (Exception e)
		{
			logger.LogWarning(e, "Cache warmup failed; types will be populated lazily on first GetAll");
		}
	}
}