using Shared.Logging;
using Bridge;
using Shared.GrpcClient;
using Shared.Grpc;

var builder = Host.CreateApplicationBuilder(args);

builder.AddTradeLogging("bridge");

builder.Services.AddTradeGrpcClient<FixProcessing.FixProcessingClient>("FIXPROCESSOR_URL");

builder.Services.AddHostedService<FixProcessWorker>();

var host = builder.Build();

host.Run();
