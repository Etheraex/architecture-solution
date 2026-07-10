using Shared.Logging;
using Shared.Grpc;
using Shared.GrpcClient;
using Shared.Web.WebServiceExtensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddTradeLogging("rest-api-service");

builder.Services.AddTradeGrpcClient<OrderPersistence.OrderPersistenceClient>("ORDERSERVICE_URL");

builder.Services.AddTradeGrpcClient<ConfigurationPersistance.ConfigurationPersistanceClient>("CONFIGURATIONSERVICE_URL");

builder.Services.AddControllers();

var app = builder.Build();

app.UseSerilogRequestLoggingConfig();

app.MapControllers();

app.Logger.LogInformation("rest-api-service started");

app.Run();