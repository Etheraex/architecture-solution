using FixProcessor.Models;
using FixProcessor.Parser;
using FixBackendShared.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/process", (FixProcessRequest request, ILogger<Program> logger) =>
{
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