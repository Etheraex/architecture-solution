using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradeData;
using Serilog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Shared.Web.WebServiceExtensions;

public static class WebServiceExtensions
{
	public static WebApplicationBuilder SetHttp2KestrelConfig(this WebApplicationBuilder builder)
	{
		builder.WebHost.ConfigureKestrel(options =>
			options.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));

		return builder;
	}

	public static WebApplicationBuilder AddTradeDbContext(this WebApplicationBuilder builder)
	{
		builder.Services.AddDbContext<TradeDbContext>(
			options => options.UseSqlServer(
				Environment.GetEnvironmentVariable("TRADE_DB_CONNECTION")
					?? throw new InvalidOperationException("TRADE_DB_CONNECTION environment variable is not set."),
			sql => sql.EnableRetryOnFailure()));

		return builder;
	}

	public static void UseSerilogRequestLoggingConfig(this WebApplication app)
	{
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
	}
}
