using Microsoft.AspNetCore.Mvc;
using TradeData;
using TradeData.Entities;

namespace ConfigurationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrokerController : ConfigurationEntityControllerBase<BrokerEntity>
{
	public BrokerController(TradeDbContext db, ILogger<BrokerController> logger)
		: base(db, logger) { }
}
