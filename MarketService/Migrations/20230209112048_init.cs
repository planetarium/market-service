using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarketService.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    productid = table.Column<Guid>(type: "uuid", nullable: false),
                    selleragentaddress = table.Column<string>(type: "text", nullable: false),
                    selleravataraddress = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    registeredblockindex = table.Column<long>(type: "bigint", nullable: false),
                    exist = table.Column<bool>(type: "boolean", nullable: false),
                    legacy = table.Column<bool>(type: "boolean", nullable: false),
                    producttype = table.Column<string>(name: "product_type", type: "text", nullable: false),
                    decimalplaces = table.Column<byte>(type: "smallint", nullable: true),
                    ticker = table.Column<string>(type: "text", nullable: true),
                    itemid = table.Column<int>(type: "integer", nullable: true),
                    grade = table.Column<int>(type: "integer", nullable: true),
                    itemtype = table.Column<int>(type: "integer", nullable: true),
                    itemsubtype = table.Column<int>(type: "integer", nullable: true),
                    elementaltype = table.Column<int>(type: "integer", nullable: true),
                    tradableid = table.Column<Guid>(type: "uuid", nullable: true),
                    setid = table.Column<int>(type: "integer", nullable: true),
                    combatpoint = table.Column<int>(type: "integer", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.productid);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    skillid = table.Column<int>(type: "integer", nullable: false),
                    elementaltype = table.Column<int>(type: "integer", nullable: false),
                    skillcategory = table.Column<int>(type: "integer", nullable: false),
                    hitcount = table.Column<int>(type: "integer", nullable: false),
                    cooldown = table.Column<int>(type: "integer", nullable: false),
                    power = table.Column<int>(type: "integer", nullable: false),
                    chance = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skills", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    value = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    additional = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stats", x => x.id);
                });

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
                name: "ix_itemproductmodelskillmodel_skillsid",
                table: "itemproductmodelskillmodel",
                column: "skillsid");

            migrationBuilder.CreateIndex(
                name: "ix_itemproductmodelstatmodel_statsid",
                table: "itemproductmodelstatmodel",
                column: "statsid");

            migrationBuilder.CreateIndex(
                name: "ix_products_exist",
                table: "products",
                column: "exist");

            migrationBuilder.CreateIndex(
                name: "ix_skills_skillid_power_chance",
                table: "skills",
                columns: new[] { "skillid", "power", "chance" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stats_value_type_additional",
                table: "stats",
                columns: new[] { "value", "type", "additional" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itemproductmodelskillmodel");

            migrationBuilder.DropTable(
                name: "itemproductmodelstatmodel");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "stats");
        }
    }
}
