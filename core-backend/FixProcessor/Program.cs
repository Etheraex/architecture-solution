using Shared.Logging;
using Shared.Grpc;
using Shared.GrpcClient;
using FixProcessor.Services;
using Shared.Web.WebServiceExtensions;

var builder = WebApplication.CreateBuilder(args);

builder
	.AddTradeLogging("fix-processor")
	.SetHttp2KestrelConfig();

builder.Services.AddTradeGrpcClient<OrderPersistence.OrderPersistenceClient>("ORDERSERVICE_URL");

builder.Services.AddGrpc();

var app = builder.Build();

app.UseSerilogRequestLoggingConfig();

app.MapGrpcService<FixProcessingService>();

app.Logger.LogInformation("fix-processor started");

app.Run();