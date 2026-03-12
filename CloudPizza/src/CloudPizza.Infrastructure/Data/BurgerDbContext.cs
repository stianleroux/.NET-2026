
using CloudBurger.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudBurger.Infrastructure.Data;
/// <summary>
/// Database context for CloudBurger application.
/// Uses primary constructor for DI (new in .NET 10).
/// </summary>
public sealed class BurgerDbContext(DbContextOptions<BurgerDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");

            // Configure strongly-typed ID
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasConversion(
                    id => id.Value,  // To database
                    value => new OrderId { Value = value })  // From database
                .ValueGeneratedNever();  // We generate IDs in the domain

            entity.Property(e => e.CustomerName)
                .HasColumnName("customer_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.BurgerType)
                .HasColumnName("burger_type")
                .HasConversion<string>()  // Store enum as string for readability
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Quantity)
                .HasColumnName("quantity")
                .IsRequired();

            entity.Property(e => e.TotalPrice)
                .HasColumnName("total_price")
                .HasPrecision(10, 2)
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            // Index for querying recent orders
            entity.HasIndex(e => e.CreatedAtUtc)
                .HasDatabaseName("ix_orders_created_at_utc");
        });

        // Add the PostgreSQL NOTIFY trigger function and trigger
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.HasPostgresExtension("uuid-ossp");
    }
}
