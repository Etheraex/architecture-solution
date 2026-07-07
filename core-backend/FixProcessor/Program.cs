using Serilog;
using FixBackendShared.Logging;
using FixBackendShared.Grpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using FixProcessor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddSerilog(lc => lc
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("service", "fix-processor")
	.WriteTo.Console(new SlogJsonFormatter()));

builder.WebHost.ConfigureKestrel(options =>
	options.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));

builder.Services.AddGrpcClient<OrderPersistence.OrderPersistenceClient>(options =>
	options.Address = new Uri(
		Environment.GetEnvironmentVariable("ORDERSERVICE_URL")
			?? throw new InvalidOperationException("ORDERSERVICE_URL environment variable is not set.")
	));

builder.Services.AddGrpc();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
	options.MessageTemplate = "HTTP request";
	options.EnrichDiagnosticContext = (diag, http) =>
	{
		diag.Set("method", http.Request.Method);
		diag.Set("path", http.Request.Path);
		diag.Set("status", http.Response.StatusCode);
	};
});

app.MapGrpcService<FixProcessingService>();

app.Logger.LogInformation("fix-processor started");

app.Run();