using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketService.Migrations
{
    /// <inheritdoc />
    public partial class AddIconId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "iconid",
                table: "products",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "iconid",
                table: "products");
        }
    }
}
