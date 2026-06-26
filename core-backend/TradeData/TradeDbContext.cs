using Microsoft.EntityFrameworkCore;
using TradeData.Entities;

namespace TradeData;

public class TradeDbContext(DbContextOptions<TradeDbContext> options) : DbContext(options)
{
	public DbSet<Order> Orders => Set<Order>();

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
		});

		base.OnModelCreating(modelBuilder);
	}
}