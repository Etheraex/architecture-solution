using FixBackendShared.Logging;
using Bridge;
using Serilog;
using FixBackendShared.Grpc;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();

builder.Services.AddSerilog(lc => lc
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("service", "bridge")
	.WriteTo.Console(new SlogJsonFormatter()));

builder.Services.AddGrpcClient<FixProcessing.FixProcessingClient>(options =>
	options.Address = new Uri(
		Environment.GetEnvironmentVariable("FIXPROCESSOR_URL")
			?? throw new InvalidOperationException("FIXPROCESSOR_URL environment variable is not set.")
	));

builder.Services.AddHostedService<FixProcessWorker>();

var host = builder.Build();

host.Run();
