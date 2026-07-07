namespace TradeData.Entities;

public abstract class ConfigurationEntity
{
	public int Id { get; set; }
	public string Code { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	public override string ToString()
	{
		string className = this.GetType().Name;
		return $"{className}: [{this.Id}] : [{this.Code}] : [{this.Description}]";
	}
}

public class StrategyEntity() : ConfigurationEntity;
public class ManagerEntity() : ConfigurationEntity;
public class FundEntity() : ConfigurationEntity;
public class BrokerEntity() : ConfigurationEntity;
public class ExchangeEntity() : ConfigurationEntity;