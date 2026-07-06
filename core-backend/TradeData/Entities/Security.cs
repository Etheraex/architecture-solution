namespace TradeData.Entities;

public class Security
{
	public int Id { get; set; }
	public required string Ticker { get; set; }
	public required string Description { get; set; }
	public int ExchangeId { get; set; }
	public ExchangeEntity Exchange { get; set; } = null!;
	public List<Order> Orders { get; set; } = new List<Order>();
}