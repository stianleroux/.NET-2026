namespace CloudBurger.Infrastructure.Migrations;

using CloudBurger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

[DbContext(typeof(BurgerDbContext))]
partial class BurgerDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "uuid-ossp");
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("CloudBurger.Shared.Domain.Order", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedAtUtc")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at_utc");

                b.Property<string>("CustomerName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)")
                    .HasColumnName("customer_name");

                b.Property<string>("BurgerType")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)")
                    .HasColumnName("burger_type");

                b.Property<int>("Quantity")
                    .HasColumnType("integer")
                    .HasColumnName("quantity");

                b.Property<decimal>("TotalPrice")
                    .HasPrecision(10, 2)
                    .HasColumnType("numeric(10,2)")
                    .HasColumnName("total_price");

                b.HasKey("Id");

                b.HasIndex("CreatedAtUtc")
                    .HasDatabaseName("ix_orders_created_at_utc");

                b.ToTable("orders", (string)null);
            });
#pragma warning restore 612, 618
    }
}
