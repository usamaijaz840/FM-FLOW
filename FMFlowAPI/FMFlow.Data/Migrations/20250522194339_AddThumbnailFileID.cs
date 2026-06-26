using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailFileID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ThumbnailFileID",
                table: "FileToEstimates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FileToEstimates_ThumbnailFileID",
                table: "FileToEstimates",
                column: "ThumbnailFileID");

            migrationBuilder.AddForeignKey(
                name: "FK_FileToEstimates_FileItems_ThumbnailFileID",
                table: "FileToEstimates",
                column: "ThumbnailFileID",
                principalTable: "FileItems",
                principalColumn: "FileID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileToEstimates_FileItems_ThumbnailFileID",
                table: "FileToEstimates");

            migrationBuilder.DropIndex(
                name: "IX_FileToEstimates_ThumbnailFileID",
                table: "FileToEstimates");

            migrationBuilder.DropColumn(
                name: "ThumbnailFileID",
                table: "FileToEstimates");
        }
    }
}
