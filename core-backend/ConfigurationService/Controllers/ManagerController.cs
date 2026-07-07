using Microsoft.AspNetCore.Mvc;
using TradeData;
using TradeData.Entities;

namespace ConfigurationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ManagerController : ConfigurationEntityControllerBase<ManagerEntity>
{
	public ManagerController(TradeDbContext db, ILogger<ManagerController> logger)
		: base(db, logger) { }
}
