using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIDToLead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerID",
                table: "Leads",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CustomerID",
                table: "Leads",
                column: "CustomerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_FMFlowUsers_CustomerID",
                table: "Leads",
                column: "CustomerID",
                principalTable: "FMFlowUsers",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_FMFlowUsers_CustomerID",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_CustomerID",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "CustomerID",
                table: "Leads");
        }
    }
}
