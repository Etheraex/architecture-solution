using Microsoft.Extensions.DependencyInjection;

namespace Shared.GrpcClient;

public static class GrpcClientExtensions
{
	public static IHttpClientBuilder AddTradeGrpcClient<TClient>(this IServiceCollection services, string urlEnvVar)
		where TClient : class
	{
		return services.AddGrpcClient<TClient>(o =>
			o.Address = new Uri(Environment.GetEnvironmentVariable(urlEnvVar)
				?? throw new InvalidOperationException($"{urlEnvVar} environment variable is not set.")));
	}
}