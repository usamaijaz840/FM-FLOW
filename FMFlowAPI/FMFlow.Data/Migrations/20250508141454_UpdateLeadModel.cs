using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLeadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Leads");

            migrationBuilder.AddColumn<int>(
                name: "AddressID",
                table: "Leads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_AddressID",
                table: "Leads",
                column: "AddressID");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Addresses_AddressID",
                table: "Leads",
                column: "AddressID",
                principalTable: "Addresses",
                principalColumn: "AddressID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Addresses_AddressID",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_AddressID",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AddressID",
                table: "Leads");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Leads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Leads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Leads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Leads",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
