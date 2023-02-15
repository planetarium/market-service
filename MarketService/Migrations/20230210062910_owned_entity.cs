using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketService.Migrations
{
    /// <inheritdoc />
    public partial class ownedentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itemproductmodelskillmodel");

            migrationBuilder.DropTable(
                name: "itemproductmodelstatmodel");

            migrationBuilder.DropPrimaryKey(
                name: "pk_stats",
                table: "stats");

            migrationBuilder.DropIndex(
                name: "ix_stats_value_type_additional",
                table: "stats");

            migrationBuilder.DropPrimaryKey(
                name: "pk_skills",
                table: "skills");

            migrationBuilder.DropIndex(
                name: "ix_skills_skillid_power_chance",
                table: "skills");

            migrationBuilder.RenameTable(
                name: "stats",
                newName: "statmodel");

            migrationBuilder.RenameTable(
                name: "skills",
                newName: "skillmodel");

            migrationBuilder.AddColumn<Guid>(
                name: "productid",
                table: "statmodel",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "productid",
                table: "skillmodel",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "pk_statmodel",
                table: "statmodel",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_skillmodel",
                table: "skillmodel",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_statmodel_productid",
                table: "statmodel",
                column: "productid");

            migrationBuilder.CreateIndex(
                name: "ix_skillmodel_productid",
                table: "skillmodel",
                column: "productid");

            migrationBuilder.AddForeignKey(
                name: "fk_skillmodel_products_itemproductmodelproductid",
                table: "skillmodel",
                column: "productid",
                principalTable: "products",
                principalColumn: "productid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_statmodel_products_itemproductmodelproductid",
                table: "statmodel",
                column: "productid",
                principalTable: "products",
                principalColumn: "productid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_skillmodel_products_itemproductmodelproductid",
                table: "skillmodel");

            migrationBuilder.DropForeignKey(
                name: "fk_statmodel_products_itemproductmodelproductid",
                table: "statmodel");

            migrationBuilder.DropPrimaryKey(
                name: "pk_statmodel",
                table: "statmodel");

            migrationBuilder.DropIndex(
                name: "ix_statmodel_productid",
                table: "statmodel");

            migrationBuilder.DropPrimaryKey(
                name: "pk_skillmodel",
                table: "skillmodel");

            migrationBuilder.DropIndex(
                name: "ix_skillmodel_productid",
                table: "skillmodel");

            migrationBuilder.DropColumn(
                name: "productid",
                table: "statmodel");

            migrationBuilder.DropColumn(
                name: "productid",
                table: "skillmodel");

            migrationBuilder.RenameTable(
                name: "statmodel",
                newName: "stats");

            migrationBuilder.RenameTable(
                name: "skillmodel",
                newName: "skills");

            migrationBuilder.AddPrimaryKey(
                name: "pk_stats",
                table: "stats",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_skills",
                table: "skills",
                column: "id");

            migrationBuilder.CreateTable(
                name: "itemproductmodelskillmodel",
                columns: table => new
                {
                    itemproductsproductid = table.Column<Guid>(type: "uuid", nullable: false),
                    skillsid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itemproductmodelskillmodel", x => new { x.itemproductsproductid, x.skillsid });
                    table.ForeignKey(
                        name: "fk_itemproductmodelskillmodel_products_itemproductsproductid",
                        column: x => x.itemproductsproductid,
                        principalTable: "products",
                        principalColumn: "productid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_itemproductmodelskillmodel_skills_skillsid",
                        column: x => x.skillsid,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itemproductmodelstatmodel",
                columns: table => new
                {
                    itemproductsproductid = table.Column<Guid>(type: "uuid", nullable: false),
                    statsid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itemproductmodelstatmodel", x => new { x.itemproductsproductid, x.statsid });
                    table.ForeignKey(
                        name: "fk_itemproductmodelstatmodel_products_itemproductsproductid",
                        column: x => x.itemproductsproductid,
                        principalTable: "products",
                        principalColumn: "productid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_itemproductmodelstatmodel_stats_statsid",
                        column: x => x.statsid,
                        principalTable: "stats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stats_value_type_additional",
                table: "stats",
                columns: new[] { "value", "type", "additional" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skills_skillid_power_chance",
                table: "skills",
                columns: new[] { "skillid", "power", "chance" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_itemproductmodelskillmodel_skillsid",
                table: "itemproductmodelskillmodel",
                column: "skillsid");

            migrationBuilder.CreateIndex(
                name: "ix_itemproductmodelstatmodel_statsid",
                table: "itemproductmodelstatmodel",
                column: "statsid");
        }
    }
}
