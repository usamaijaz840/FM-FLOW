using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManualLeadsAssignedToPros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProUserID",
                table: "Leads",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ProUserID",
                table: "Leads",
                column: "ProUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_FMFlowUsers_ProUserID",
                table: "Leads",
                column: "ProUserID",
                principalTable: "FMFlowUsers",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_FMFlowUsers_ProUserID",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_ProUserID",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ProUserID",
                table: "Leads");
        }
    }
}
