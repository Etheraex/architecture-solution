namespace RestApiService.Models;

public record OrderResponse(int Id, string OrderId, string Symbol, string Side, decimal Quantity, decimal Price);