namespace FixProcessor.Models;

public enum OrderSide { Buy, Sell }

public record Order(
	string OrderId,
	string SecurityId,
	OrderSide Side,
	decimal Quantity,
	decimal Price
);