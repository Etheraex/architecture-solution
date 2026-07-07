using Microsoft.EntityFrameworkCore;
using TradeData.Entities;

namespace TradeData;

public class TradeDbContext(DbContextOptions<TradeDbContext> options) : DbContext(options)
{
	public DbSet<Order> Orders => Set<Order>();
	public DbSet<StrategyEntity> Strategies => Set<StrategyEntity>();
	public DbSet<ManagerEntity> Managers => Set<ManagerEntity>();
	public DbSet<FundEntity> Funds => Set<FundEntity>();
	public DbSet<ExchangeEntity> Exchanges => Set<ExchangeEntity>();
	public DbSet<BrokerEntity> Brokers => Set<BrokerEntity>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<OrderSideLookup>(e =>
		{
			e.Property(s => s.Id).ValueGeneratedNever();
			e.Property(s => s.Display).HasMaxLength(20);
			e.HasData(
				new OrderSideLookup { Id = OrderSide.None, Display = "None" },
				new OrderSideLookup { Id = OrderSide.Buy, Display = "Buy" },
				new OrderSideLookup { Id = OrderSide.Sell, Display = "Sell" }
			);
		});

		modelBuilder.Entity<Order>(e =>
		{
			e.HasIndex(o => o.OrderId).IsUnique();
			e.Property(o => o.Quantity).HasPrecision(18, 4);
			e.Property(o => o.Price).HasPrecision(18, 4);
			
			e.HasOne(o => o.SideType)
				.WithMany()
				.HasForeignKey(o => o.Side)
				.OnDelete(DeleteBehavior.Restrict);

			e.HasOne(o => o.Strategy)
				.WithMany()
				.HasForeignKey(o => o.StrategyId)
				.OnDelete(DeleteBehavior.Restrict);

			e.HasOne(o => o.Manager)
				.WithMany()
				.HasForeignKey(o => o.ManagerId)
				.OnDelete(DeleteBehavior.Restrict);

			e.HasOne(o => o.Fund)
				.WithMany()
				.HasForeignKey(o => o.FundId)
				.OnDelete(DeleteBehavior.Restrict);

			e.HasOne(o => o.Broker)
				.WithMany()
				.HasForeignKey(o => o.BrokerId)
				.OnDelete(DeleteBehavior.Restrict);

			e.HasOne(o => o.Security)
				.WithMany(s => s.Orders)
				.HasForeignKey(o => o.SecurityId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<Security>(e =>
		{
			e.HasIndex(o => o.Ticker).IsUnique();
			e.Property(o => o.Ticker).HasMaxLength(50);
			e.Property(o => o.Description).HasMaxLength(100);

			e.HasOne(o => o.Exchange)
				.WithMany()
				.HasForeignKey(o => o.ExchangeId)
				.OnDelete(DeleteBehavior.Restrict);

			e.HasData(
				new Security { Id = 1,  Ticker = "AAPL",  Description = "Apple Inc.",           ExchangeId = 2 },
				new Security { Id = 2,  Ticker = "MSFT",  Description = "Microsoft Corp.",      ExchangeId = 2 },
				new Security { Id = 3,  Ticker = "AMZN",  Description = "Amazon.com Inc.",      ExchangeId = 2 },
				new Security { Id = 4,  Ticker = "GOOGL", Description = "Alphabet Inc.",        ExchangeId = 2 },
				new Security { Id = 5,  Ticker = "META",  Description = "Meta Platforms Inc.",  ExchangeId = 2 },
				new Security { Id = 6,  Ticker = "NVDA",  Description = "NVIDIA Corp.",         ExchangeId = 2 },
				new Security { Id = 7,  Ticker = "TSLA",  Description = "Tesla Inc.",           ExchangeId = 2 },
				new Security { Id = 8,  Ticker = "NFLX",  Description = "Netflix Inc.",         ExchangeId = 2 },
				new Security { Id = 9,  Ticker = "JPM",   Description = "JPMorgan Chase & Co.", ExchangeId = 3 },
				new Security { Id = 10, Ticker = "V",     Description = "Visa Inc.",            ExchangeId = 3 }
			);
		});

		#region Configuration Entities

		modelBuilder.Entity<StrategyEntity>(e =>
		{
			e.Property(o => o.Code).HasMaxLength(20);
			e.Property(o => o.Description).HasMaxLength(100);
			e.HasData(new StrategyEntity() { Id = 1, Code = "*None*", Description = "System Default Strategy"});
		});

		modelBuilder.Entity<ManagerEntity>(e =>
		{
			e.Property(o => o.Code).HasMaxLength(20);
			e.Property(o => o.Description).HasMaxLength(100);
			e.HasData(new ManagerEntity() { Id = 1, Code = "*None*", Description = "System Default Manager"});
		});

		modelBuilder.Entity<FundEntity>(e =>
		{
			e.Property(o => o.Code).HasMaxLength(20);
			e.Property(o => o.Description).HasMaxLength(100);
			e.HasData(new FundEntity() { Id = 1, Code = "*None*", Description = "System Default Fund"});
		});

		modelBuilder.Entity<BrokerEntity>(e =>
		{
			e.Property(o => o.Code).HasMaxLength(20);
			e.Property(o => o.Description).HasMaxLength(100);
			e.HasData(new BrokerEntity() { Id = 1, Code = "*None*", Description = "System Default Broker"});
		});

		modelBuilder.Entity<ExchangeEntity>(e =>
		{
			e.Property(o => o.Code).HasMaxLength(20);
			e.Property(o => o.Description).HasMaxLength(100);
			e.HasData(
				new ExchangeEntity() { Id = 1, Code = "*None*", Description = "System Default Exchange"},
				new ExchangeEntity() { Id = 2, Code = "NASDAQ", Description = "Nasdaq Stock Market" },
				new ExchangeEntity() { Id = 3, Code = "NYSE",   Description = "New York Stock Exchange" });
		});

		#endregion

		base.OnModelCreating(modelBuilder);
	}
}