using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaintSheenPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProUserId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaintSheens",
                columns: table => new
                {
                    PaintSheenId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SheenId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProUserId = table.Column<int>(type: "integer", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaintSheens", x => x.PaintSheenId);
                    table.ForeignKey(
                        name: "FK_PaintSheens_FMFlowUsers_ProUserId",
                        column: x => x.ProUserId,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_PaintSheens_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaintSheens_Sheens_SheenId",
                        column: x => x.SheenId,
                        principalTable: "Sheens",
                        principalColumn: "SheenID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaintPrices",
                columns: table => new
                {
                    PaintPriceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaintSheenId = table.Column<int>(type: "integer", nullable: false),
                    ProUserId = table.Column<int>(type: "integer", nullable: false),
                    PricePerGallon = table.Column<decimal>(type: "numeric", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaintPrices", x => x.PaintPriceId);
                    table.ForeignKey(
                        name: "FK_PaintPrices_FMFlowUsers_ProUserId",
                        column: x => x.ProUserId,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaintPrices_PaintSheens_PaintSheenId",
                        column: x => x.PaintSheenId,
                        principalTable: "PaintSheens",
                        principalColumn: "PaintSheenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProUserId",
                table: "Products",
                column: "ProUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaintPrices_PaintSheenId",
                table: "PaintPrices",
                column: "PaintSheenId");

            migrationBuilder.CreateIndex(
                name: "IX_PaintPrices_ProUserId",
                table: "PaintPrices",
                column: "ProUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaintSheens_ProductId",
                table: "PaintSheens",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PaintSheens_ProUserId",
                table: "PaintSheens",
                column: "ProUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaintSheens_SheenId",
                table: "PaintSheens",
                column: "SheenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_FMFlowUsers_ProUserId",
                table: "Products",
                column: "ProUserId",
                principalTable: "FMFlowUsers",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_FMFlowUsers_ProUserId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "PaintPrices");

            migrationBuilder.DropTable(
                name: "PaintSheens");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProUserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProUserId",
                table: "Products");
        }
    }
}
