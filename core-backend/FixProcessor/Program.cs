using Serilog;
using FixProcessor.Parser;
using FixBackendShared.Models;
using FixBackendShared.Logging;
using FixBackendShared.Grpc;
using Grpc.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddSerilog(lc => lc
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("service", "fix-processor")
	.WriteTo.Console(new SlogJsonFormatter()));

builder.Services.AddGrpcClient<OrderPersistence.OrderPersistenceClient>(options =>
	options.Address = new Uri(
		Environment.GetEnvironmentVariable("ORDERSERVICE_URL")
			?? throw new InvalidOperationException("ORDERSERVICE_URL environment variable is not set.")
	));

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

app.MapPost("/process", async (
	FixProcessRequest request,
	OrderPersistence.OrderPersistenceClient orderClient,
	ILogger<Program> logger,
	CancellationToken cancellationToken) =>
{
	using var _ = logger.BeginScope(new Dictionary<string, object> { ["fixId"] = request.Id });

	PersistOrderRequest orderRequest;

	try
	{
		orderRequest = FixParser.ToPersistRequest(request);
	}
	catch (FormatException fe)
	{
		logger.LogWarning(fe, "Unparseable FIX {Id}", request.Id);
		return Results.UnprocessableEntity(new { request.Id, error = fe.Message });
	}

	try
	{
		var response = await orderClient.PersistAsync(orderRequest, cancellationToken: cancellationToken);
		logger.LogInformation(
			response.AlreadyExisted
				? "Order {OrderId} already persisted, treating as success"
				: "Persisted order {OrderId}",
			orderRequest.OrderId);
	}
	catch (RpcException rpc) when (rpc.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument)
	{
		logger.LogWarning("Order {OrderId} rejected: {Detail}", orderRequest.OrderId, rpc.Status.Detail);
		return Results.UnprocessableEntity(new { request.Id, error = rpc.Status.Detail });
	}

	return Results.Text(request.Id);
});

app.Run();