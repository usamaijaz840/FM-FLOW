using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectProCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProId",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProId",
                table: "Projects",
                column: "ProId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_FMFlowUsers_ProId",
                table: "Projects",
                column: "ProId",
                principalTable: "FMFlowUsers",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_FMFlowUsers_ProId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProId",
                table: "Projects");
        }
    }
}
