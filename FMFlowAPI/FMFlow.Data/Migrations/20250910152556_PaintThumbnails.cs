using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaintThumbnails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ThumbnailFileId",
                table: "Paints",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paints_ThumbnailFileId",
                table: "Paints",
                column: "ThumbnailFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Paints_FileItems_ThumbnailFileId",
                table: "Paints",
                column: "ThumbnailFileId",
                principalTable: "FileItems",
                principalColumn: "FileID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Paints_FileItems_ThumbnailFileId",
                table: "Paints");

            migrationBuilder.DropIndex(
                name: "IX_Paints_ThumbnailFileId",
                table: "Paints");

            migrationBuilder.DropColumn(
                name: "ThumbnailFileId",
                table: "Paints");
        }
    }
}
