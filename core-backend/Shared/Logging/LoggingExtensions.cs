using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Shared.Logging;

public static class LoggingExtensions
{
	public static T AddTradeLogging<T>(this T b, string serviceName) where T
		: IHostApplicationBuilder
	{
		b.Logging.ClearProviders();
		b.Services.AddSerilog(lc => lc
			.MinimumLevel.Information()
			.Enrich.FromLogContext()
			.Enrich.WithProperty("service", serviceName)
			.WriteTo.Console(new SlogJsonFormatter()));
		return b;
	}
}