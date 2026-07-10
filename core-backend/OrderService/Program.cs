using Shared.Logging;
using Microsoft.EntityFrameworkCore;
using OrderService.Services;
using TradeData;
using Shared.Web.WebServiceExtensions;

var builder = WebApplication.CreateBuilder(args);

builder
	.AddTradeLogging("order-service")
	.SetHttp2KestrelConfig()
	.AddTradeDbContext();

builder.Services.AddGrpc();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
	db.Database.Migrate();
}

app.UseSerilogRequestLoggingConfig();

app.MapGrpcService<OrderPersistenceService>();

app.Logger.LogInformation("order-service started");

app.Run();