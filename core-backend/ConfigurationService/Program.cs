using ConfigurationService.Services;
using Shared.Logging;
using Shared.Web.WebServiceExtensions;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder
	.AddTradeLogging("configuration-service")
	.SetHttp2KestrelConfig()
	.AddTradeDbContext();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
	var connection = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
		?? throw new InvalidOperationException("REDIS_CONNECTION environment variable is not set.");

	var options = ConfigurationOptions.Parse(connection);
	options.AbortOnConnectFail = false;
	options.BacklogPolicy = BacklogPolicy.FailFast;

	return ConnectionMultiplexer.Connect(options);;
});

builder.Services.AddSingleton<ConfigurationCache>();
builder.Services.AddHostedService<ConfigurationCacheWarmup>();

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<ConfigurationPersistenceService>();

app.UseSerilogRequestLoggingConfig();

app.Logger.LogInformation("configuration-service started");

app.Run();