using Microsoft.AspNetCore.Mvc;
using TradeData;
using TradeData.Entities;

namespace ConfigurationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FundController : ConfigurationEntityControllerBase<FundEntity>
{
	public FundController(TradeDbContext db, ILogger<FundController> logger)
		: base(db, logger) { }
}
