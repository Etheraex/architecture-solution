using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TradeData;

public class TradeDbContextFactory : IDesignTimeDbContextFactory<TradeDbContext>
{
	public TradeDbContext CreateDbContext(string[] args)
	{
		var connectionString =
			Environment.GetEnvironmentVariable("TRADE_DB_CONNECTION");

		if (string.IsNullOrEmpty(connectionString))
			throw new InvalidOperationException("TRADE_DB_CONNECTION environment variable is not set.");

		var options = new DbContextOptionsBuilder<TradeDbContext>()
			.UseSqlServer(connectionString)
			.Options;

		return new TradeDbContext(options);
	}
}