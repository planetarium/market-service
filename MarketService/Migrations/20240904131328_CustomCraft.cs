using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketService.Migrations
{
    /// <inheritdoc />
    public partial class CustomCraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "bycustomcraft",
                table: "products",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "hasrandomonlyicon",
                table: "products",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("UPDATE products SET bycustomcraft = FALSE WHERE product_type = 'item'");
            migrationBuilder.Sql("UPDATE products SET hasrandomonlyicon = FALSE where product_type = 'item'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bycustomcraft",
                table: "products");

            migrationBuilder.DropColumn(
                name: "hasrandomonlyicon",
                table: "products");
        }
    }
}
