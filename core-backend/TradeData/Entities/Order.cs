namespace TradeData.Entities;

public enum OrderSide
{
	None = 0,
	Buy = 1,
	Sell = 2
}

public class OrderSideLookup
{
	public OrderSide Id { get; set; }
	public required string Display { get; set; }
}

public class Order
{
	public int Id { get; set; }
	public required string OrderId { get; set; }
	public required string SecurityId { get; set; }
	public OrderSide Side { get; set; }
	public OrderSideLookup SideType { get; set; } = null!;
	public decimal Quantity { get; set; }
	public decimal Price { get; set; }
}