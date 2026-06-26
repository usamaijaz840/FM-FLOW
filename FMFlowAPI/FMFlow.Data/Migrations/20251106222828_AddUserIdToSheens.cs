using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToSheens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Sheens",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sheens_UserId",
                table: "Sheens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sheens_FMFlowUsers_UserId",
                table: "Sheens",
                column: "UserId",
                principalTable: "FMFlowUsers",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sheens_FMFlowUsers_UserId",
                table: "Sheens");

            migrationBuilder.DropIndex(
                name: "IX_Sheens_UserId",
                table: "Sheens");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Sheens");
        }
    }
}
