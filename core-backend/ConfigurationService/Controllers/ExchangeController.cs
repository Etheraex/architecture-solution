using Microsoft.AspNetCore.Mvc;
using TradeData;
using TradeData.Entities;

namespace ConfigurationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExchangeController : ConfigurationEntityControllerBase<ExchangeEntity>
{
	public ExchangeController(TradeDbContext db, ILogger<ExchangeController> logger)
		: base(db, logger) { }
}
