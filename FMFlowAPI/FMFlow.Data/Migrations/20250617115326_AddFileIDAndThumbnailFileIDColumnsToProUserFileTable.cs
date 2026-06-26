using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileIDAndThumbnailFileIDColumnsToProUserFileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ThumbnailFileID",
                table: "ProUserFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProUserFiles_ThumbnailFileID",
                table: "ProUserFiles",
                column: "ThumbnailFileID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserFiles_FileItems_ThumbnailFileID",
                table: "ProUserFiles",
                column: "ThumbnailFileID",
                principalTable: "FileItems",
                principalColumn: "FileID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProUserFiles_FileItems_ThumbnailFileID",
                table: "ProUserFiles");

            migrationBuilder.DropIndex(
                name: "IX_ProUserFiles_ThumbnailFileID",
                table: "ProUserFiles");

            migrationBuilder.DropColumn(
                name: "ThumbnailFileID",
                table: "ProUserFiles");
        }
    }
}
