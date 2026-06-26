using FixProcessor.Parser;
using FixBackendShared.Models;
using FixBackendShared.Logging;
using Serilog;
using Microsoft.EntityFrameworkCore;
using TradeData;
using TradeData.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddSerilog(lc => lc
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("service", "fix-processor")
	.WriteTo.Console(new SlogJsonFormatter()));

builder.Services.AddDbContext<TradeDbContext>(options => 
	options.UseSqlServer(
		Environment.GetEnvironmentVariable("TRADE_DB_CONNECTION")
			?? throw new InvalidOperationException("TRADE_DB_CONNECTION environment variable is not set."),
		sql => sql.EnableRetryOnFailure()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
	db.Database.Migrate();
}

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

app.MapPost("/process", async (
	FixProcessRequest request,
	TradeDbContext db,
	ILogger<Program> logger,
	CancellationToken cancellationToken) =>
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

	db.Orders.Add(order);

	try
	{
		await db.SaveChangesAsync(cancellationToken);
		logger.LogInformation("Persisted order {OrderId}", order.OrderId);
	}
	catch (DbUpdateException dbe) when (IsUniqueValidation(dbe))
	{
		logger.LogInformation("Order {OrderId} already persisted, treating as success", order.OrderId);
	}

	return Results.Text(request.Id);
});

// (2601 = unique index violation, 2627 = unique constraint — OrderId index throws one of these.)
static bool IsUniqueValidation(DbUpdateException dbe)
	=> dbe.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };

app.Run();