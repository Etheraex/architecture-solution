namespace TradeData.Entities;

public abstract class ConfigurationEntity
{
	public int Id { get; set; }
	public required string Code { get; set; }
	public required string Description { get; set; }
}

public class StrategyEntity() : ConfigurationEntity;
public class ManagerEntity() : ConfigurationEntity;
public class FundEntity() : ConfigurationEntity;
public class BrokerEntity() : ConfigurationEntity;
public class ExchangeEntity() : ConfigurationEntity;