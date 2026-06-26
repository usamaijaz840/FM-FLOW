using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class FileKey_And_EstimateFileType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Path",
                table: "FileItems",
                newName: "Key");

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "FileToEstimates",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileType",
                table: "FileToEstimates");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "FileItems",
                newName: "Path");
        }
    }
}
