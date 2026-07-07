using Microsoft.AspNetCore.Mvc;
using TradeData;
using TradeData.Entities;

namespace ConfigurationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StrategyController : ConfigurationEntityControllerBase<StrategyEntity>
{
	public StrategyController(TradeDbContext db, ILogger<StrategyController> logger)
		: base(db, logger) { }
}
