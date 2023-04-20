using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketService.Migrations
{
    /// <inheritdoc />
    public partial class addunitprice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "unitprice",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
            migrationBuilder.Sql("UPDATE products set unitprice = price / quantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "unitprice",
                table: "products");
        }
    }
}
