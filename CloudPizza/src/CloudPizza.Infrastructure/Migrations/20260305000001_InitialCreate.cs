namespace CloudBurger.Infrastructure.Migrations;

using CloudBurger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

[DbContext(typeof(BurgerDbContext))]
[Migration("20260305000001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

        migrationBuilder.CreateTable(
            name: "orders",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                burger_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                total_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_orders", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_orders_created_at_utc",
            table: "orders",
            column: "created_at_utc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "orders");
    }
}
