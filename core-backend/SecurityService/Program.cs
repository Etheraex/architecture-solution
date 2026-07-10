using Shared.Logging;
using Shared.Web.WebServiceExtensions;

var builder = WebApplication.CreateBuilder(args);

builder
	.AddTradeLogging("security-service")
	.AddTradeDbContext();

var app = builder.Build();

app.UseSerilogRequestLoggingConfig();

app.Logger.LogInformation("security-service started");

app.Run();