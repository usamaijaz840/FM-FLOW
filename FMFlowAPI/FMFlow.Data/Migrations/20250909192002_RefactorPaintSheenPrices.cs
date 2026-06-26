using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPaintSheenPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaintSheens_FMFlowUsers_ProUserId",
                table: "PaintSheens");

            migrationBuilder.DropForeignKey(
                name: "FK_PaintSheens_Paints_ProductId",
                table: "PaintSheens");

            migrationBuilder.DropTable(
                name: "PaintPrices");

            migrationBuilder.DropIndex(
                name: "IX_PaintSheens_ProUserId",
                table: "PaintSheens");

            migrationBuilder.DropColumn(
                name: "ProUserId",
                table: "PaintSheens");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "PaintSheens",
                newName: "PaintId");

            migrationBuilder.RenameIndex(
                name: "IX_PaintSheens_ProductId",
                table: "PaintSheens",
                newName: "IX_PaintSheens_PaintId");

            migrationBuilder.CreateTable(
                name: "PaintSheenPrices",
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
                    table.PrimaryKey("PK_PaintSheenPrices", x => x.PaintPriceId);
                    table.ForeignKey(
                        name: "FK_PaintSheenPrices_FMFlowUsers_ProUserId",
                        column: x => x.ProUserId,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaintSheenPrices_PaintSheens_PaintSheenId",
                        column: x => x.PaintSheenId,
                        principalTable: "PaintSheens",
                        principalColumn: "PaintSheenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaintSheenPrices_PaintSheenId",
                table: "PaintSheenPrices",
                column: "PaintSheenId");

            migrationBuilder.CreateIndex(
                name: "IX_PaintSheenPrices_ProUserId",
                table: "PaintSheenPrices",
                column: "ProUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaintSheens_Paints_PaintId",
                table: "PaintSheens",
                column: "PaintId",
                principalTable: "Paints",
                principalColumn: "PaintId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaintSheens_Paints_PaintId",
                table: "PaintSheens");

            migrationBuilder.DropTable(
                name: "PaintSheenPrices");

            migrationBuilder.RenameColumn(
                name: "PaintId",
                table: "PaintSheens",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_PaintSheens_PaintId",
                table: "PaintSheens",
                newName: "IX_PaintSheens_ProductId");

            migrationBuilder.AddColumn<int>(
                name: "ProUserId",
                table: "PaintSheens",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaintPrices",
                columns: table => new
                {
                    PaintPriceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaintSheenId = table.Column<int>(type: "integer", nullable: false),
                    ProUserId = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateDeleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    PricePerGallon = table.Column<decimal>(type: "numeric", nullable: false)
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
                name: "IX_PaintSheens_ProUserId",
                table: "PaintSheens",
                column: "ProUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaintPrices_PaintSheenId",
                table: "PaintPrices",
                column: "PaintSheenId");

            migrationBuilder.CreateIndex(
                name: "IX_PaintPrices_ProUserId",
                table: "PaintPrices",
                column: "ProUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaintSheens_FMFlowUsers_ProUserId",
                table: "PaintSheens",
                column: "ProUserId",
                principalTable: "FMFlowUsers",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PaintSheens_Paints_ProductId",
                table: "PaintSheens",
                column: "ProductId",
                principalTable: "Paints",
                principalColumn: "PaintId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
