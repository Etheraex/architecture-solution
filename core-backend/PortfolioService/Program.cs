using Shared.Logging;
using Shared.Web.WebServiceExtensions;

var builder = WebApplication.CreateBuilder(args);

builder
	.AddTradeLogging("portfolio-service")
	.AddTradeDbContext();

var app = builder.Build();

app.UseSerilogRequestLoggingConfig();

app.Logger.LogInformation("portfolio-service started");

app.Run();