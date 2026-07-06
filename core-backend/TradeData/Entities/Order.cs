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
	public int SecurityId { get; set; }
	public Security Security { get; set; } = null!;
	public OrderSide Side { get; set; }
	public OrderSideLookup SideType { get; set; } = null!;
	public decimal Quantity { get; set; }
	public decimal Price { get; set; }
	public int StrategyId { get; set; }
	public StrategyEntity Strategy { get; set; } = null!;
	public int FundId { get; set; }
	public FundEntity Fund { get; set; } = null!;
	public int BrokerId { get; set; }
	public BrokerEntity Broker { get; set; } = null!;
	public int ManagerId { get; set; }
	public ManagerEntity Manager { get; set; } = null!;
}