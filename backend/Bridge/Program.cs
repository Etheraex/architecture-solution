using FixBackendShared.Logging;
using Bridge;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();

builder.Services.AddSerilog(lc => lc
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("service", "bridge")
	.WriteTo.Console(new SlogJsonFormatter()));

builder.Services.AddHttpClient("fix-processor", client =>
{
	var baseUrl = Environment.GetEnvironmentVariable("FIXPROCESSOR_URL")
		?? throw new InvalidOperationException("FIXPROCESSOR_URL is not set");

	client.BaseAddress = new Uri(baseUrl);
	client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHostedService<FixProcessWorker>();

var host = builder.Build();
host.Run();
