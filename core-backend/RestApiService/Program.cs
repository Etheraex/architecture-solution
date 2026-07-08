using Serilog;
using FixBackendShared.Logging;
using FixBackendShared.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddSerilog(lc => lc
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("service", "rest-api-service")
	.WriteTo.Console(new SlogJsonFormatter()));

builder.Services.AddGrpcClient<OrderPersistence.OrderPersistenceClient>(options =>
	options.Address = new Uri(
		Environment.GetEnvironmentVariable("ORDERSERVICE_URL")
			?? throw new InvalidOperationException("ORDERSERVICE_URL environment variable is not set.")
	));

builder.Services.AddControllers();

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

app.MapControllers();

app.Logger.LogInformation("rest-api-service started");

app.Run();