using FixBackendShared.Logging;
using Serilog;
using Microsoft.EntityFrameworkCore;
using TradeData;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddSerilog(lc => lc
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("service", "security-service")
	.WriteTo.Console(new SlogJsonFormatter()));

builder.Services.AddDbContext<TradeDbContext>(options => 
	options.UseSqlServer(
		Environment.GetEnvironmentVariable("TRADE_DB_CONNECTION")
			?? throw new InvalidOperationException("TRADE_DB_CONNECTION environment variable is not set."),
		sql => sql.EnableRetryOnFailure()));

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

app.Logger.LogInformation("security-service started");

app.Run();