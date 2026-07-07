using FixBackendShared.Logging;
using Serilog;
using Microsoft.EntityFrameworkCore;
using TradeData;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddSerilog(lc => lc
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("service", "configuration-service")
	.WriteTo.Console(new SlogJsonFormatter()));

builder.Services.AddDbContext<TradeDbContext>(options => 
	options.UseSqlServer(
		Environment.GetEnvironmentVariable("TRADE_DB_CONNECTION")
			?? throw new InvalidOperationException("TRADE_DB_CONNECTION environment variable is not set."),
		sql => sql.EnableRetryOnFailure()));

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

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

app.Logger.LogInformation("configuration-service started");

app.Run();