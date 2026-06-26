using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class SherwinWilliams_PaintsAndColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cleanup",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketingCopy",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PictureFileId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductCategory",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductLineId",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SherwinWilliamsPictureURL",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Substrate",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurfacePreparation",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Warranty",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Colors",
                columns: table => new
                {
                    ColorId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SWColorId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ColorType = table.Column<string>(type: "text", nullable: true),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    PrimaryFamily = table.Column<string>(type: "text", nullable: true),
                    TintCategory = table.Column<string>(type: "text", nullable: true),
                    Top50Interior = table.Column<int>(type: "integer", nullable: true),
                    Top50Exterior = table.Column<int>(type: "integer", nullable: true),
                    PaintAreaType = table.Column<string>(type: "text", nullable: false),
                    FinesWhitesAndNeutrals = table.Column<bool>(type: "boolean", nullable: true),
                    SherwinWilliamsPictureURL = table.Column<string>(type: "text", nullable: true),
                    PictureFileId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colors", x => x.ColorId);
                    table.ForeignKey(
                        name: "FK_Colors_FileItems_PictureFileId",
                        column: x => x.PictureFileId,
                        principalTable: "FileItems",
                        principalColumn: "FileID");
                    table.ForeignKey(
                        name: "FK_Colors_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_PictureFileId",
                table: "Products",
                column: "PictureFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductLineId",
                table: "Products",
                column: "ProductLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colors_PictureFileId",
                table: "Colors",
                column: "PictureFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Colors_ProductId",
                table: "Colors",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Colors_SWColorId",
                table: "Colors",
                column: "SWColorId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_FileItems_PictureFileId",
                table: "Products",
                column: "PictureFileId",
                principalTable: "FileItems",
                principalColumn: "FileID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_FileItems_PictureFileId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Colors");

            migrationBuilder.DropIndex(
                name: "IX_Products_PictureFileId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductLineId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Cleanup",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MarketingCopy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PictureFileId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductCategory",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductLineId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SherwinWilliamsPictureURL",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Substrate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SurfacePreparation",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Warranty",
                table: "Products");
        }
    }
}
