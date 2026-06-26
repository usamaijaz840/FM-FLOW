using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class NullableThumbnailFilesOnEstimates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileToEstimates_FileItems_ThumbnailFileID",
                table: "FileToEstimates");

            migrationBuilder.AlterColumn<int>(
                name: "ThumbnailFileID",
                table: "FileToEstimates",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_FileToEstimates_FileItems_ThumbnailFileID",
                table: "FileToEstimates",
                column: "ThumbnailFileID",
                principalTable: "FileItems",
                principalColumn: "FileID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileToEstimates_FileItems_ThumbnailFileID",
                table: "FileToEstimates");

            migrationBuilder.AlterColumn<int>(
                name: "ThumbnailFileID",
                table: "FileToEstimates",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FileToEstimates_FileItems_ThumbnailFileID",
                table: "FileToEstimates",
                column: "ThumbnailFileID",
                principalTable: "FileItems",
                principalColumn: "FileID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
