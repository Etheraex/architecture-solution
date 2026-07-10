using ConfigurationService.Services;
using Shared.Logging;
using Shared.Web.WebServiceExtensions;

var builder = WebApplication.CreateBuilder(args);

builder
	.AddTradeLogging("configuration-service")
	.SetHttp2KestrelConfig()
	.AddTradeDbContext();

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<ConfigurationPersistenceService>();

app.UseSerilogRequestLoggingConfig();

app.Logger.LogInformation("configuration-service started");

app.Run();