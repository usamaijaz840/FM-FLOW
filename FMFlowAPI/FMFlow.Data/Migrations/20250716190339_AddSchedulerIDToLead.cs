using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulerIdToLead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchedulerId",
                table: "Leads",
                type: "integer",
                nullable: true);


            migrationBuilder.CreateIndex(
                name: "IX_Leads_SchedulerId",
                table: "Leads",
                column: "SchedulerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_FMFlowUsers_SchedulerId",
                table: "Leads",
                column: "SchedulerId",
                principalTable: "FMFlowUsers",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_FMFlowUsers_SchedulerId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_SchedulerId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "SchedulerId",
                table: "Leads");
        }
    }
}
