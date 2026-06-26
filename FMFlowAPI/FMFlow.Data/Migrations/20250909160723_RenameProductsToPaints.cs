using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameProductsToPaints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colors_Products_ProductId",
                table: "Colors");

            migrationBuilder.DropForeignKey(
                name: "FK_PaintSheens_Products_ProductId",
                table: "PaintSheens");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "Colors",
                newName: "PaintId");

            migrationBuilder.RenameIndex(
                name: "IX_Colors_ProductId",
                table: "Colors",
                newName: "IX_Colors_PaintId");

            migrationBuilder.CreateTable(
                name: "Paints",
                columns: table => new
                {
                    PaintId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProUserId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PaintAreaType = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProductLineId = table.Column<string>(type: "text", nullable: true),
                    ProductCategory = table.Column<string>(type: "text", nullable: true),
                    Warranty = table.Column<string>(type: "text", nullable: true),
                    Substrate = table.Column<string>(type: "text", nullable: true),
                    SurfacePreparation = table.Column<string>(type: "text", nullable: true),
                    Cleanup = table.Column<string>(type: "text", nullable: true),
                    MarketingCopy = table.Column<string>(type: "text", nullable: true),
                    SherwinWilliamsPictureURL = table.Column<string>(type: "text", nullable: true),
                    PictureFileId = table.Column<int>(type: "integer", nullable: true),
                    TintCategory = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paints", x => x.PaintId);
                    table.ForeignKey(
                        name: "FK_Paints_FMFlowUsers_ProUserId",
                        column: x => x.ProUserId,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Paints_FileItems_PictureFileId",
                        column: x => x.PictureFileId,
                        principalTable: "FileItems",
                        principalColumn: "FileID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Paints_PictureFileId",
                table: "Paints",
                column: "PictureFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Paints_ProductLineId",
                table: "Paints",
                column: "ProductLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paints_ProUserId",
                table: "Paints",
                column: "ProUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Colors_Paints_PaintId",
                table: "Colors",
                column: "PaintId",
                principalTable: "Paints",
                principalColumn: "PaintId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaintSheens_Paints_ProductId",
                table: "PaintSheens",
                column: "ProductId",
                principalTable: "Paints",
                principalColumn: "PaintId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colors_Paints_PaintId",
                table: "Colors");

            migrationBuilder.DropForeignKey(
                name: "FK_PaintSheens_Paints_ProductId",
                table: "PaintSheens");

            migrationBuilder.DropTable(
                name: "Paints");

            migrationBuilder.RenameColumn(
                name: "PaintId",
                table: "Colors",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Colors_PaintId",
                table: "Colors",
                newName: "IX_Colors_ProductId");

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PictureFileId = table.Column<int>(type: "integer", nullable: true),
                    ProUserId = table.Column<int>(type: "integer", nullable: true),
                    Cleanup = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateDeleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingCopy = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PaintAreaType = table.Column<string>(type: "text", nullable: true),
                    ProductCategory = table.Column<string>(type: "text", nullable: true),
                    ProductLineId = table.Column<string>(type: "text", nullable: true),
                    SherwinWilliamsPictureURL = table.Column<string>(type: "text", nullable: true),
                    Substrate = table.Column<string>(type: "text", nullable: true),
                    SurfacePreparation = table.Column<string>(type: "text", nullable: true),
                    TintCategory = table.Column<string>(type: "text", nullable: true),
                    Warranty = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Products_FMFlowUsers_ProUserId",
                        column: x => x.ProUserId,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Products_FileItems_PictureFileId",
                        column: x => x.PictureFileId,
                        principalTable: "FileItems",
                        principalColumn: "FileID");
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
                name: "IX_Products_ProUserId",
                table: "Products",
                column: "ProUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Colors_Products_ProductId",
                table: "Colors",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_PaintSheens_Products_ProductId",
                table: "PaintSheens",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
