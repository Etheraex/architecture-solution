using FixProcessor.Models;
using FixProcessor.Parser;
using FixBackendShared.Models;
using FixBackendShared.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddSerilog(lc => lc
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("service", "fix-processor")
	.WriteTo.Console(new SlogJsonFormatter()));

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

app.Logger.LogInformation("fix-processor started");

app.MapPost("/process", (FixProcessRequest request, ILogger<Program> logger) =>
{
	using var _ = logger.BeginScope(new Dictionary<string, object> { ["fixId"] = request.Id });

	Order order;

	try
	{
		order = FixParser.ToOrder(request);
	}
	catch (FormatException fe)
	{
		logger.LogWarning(fe, "Unparseable FIX {Id}", request.Id);
		return Results.UnprocessableEntity(new { request.Id, error = fe.Message });
	}

	logger.LogInformation("Order {OrderId}", order.OrderId);

	return Results.Ok(order);
});

app.Run();