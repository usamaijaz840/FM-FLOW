using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAddressEmployeeUsersModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "EmployeeUsers");

            migrationBuilder.AddColumn<int>(
                name: "AddressID",
                table: "EmployeeUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeUsers_AddressID",
                table: "EmployeeUsers",
                column: "AddressID");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeUsers_Addresses_AddressID",
                table: "EmployeeUsers",
                column: "AddressID",
                principalTable: "Addresses",
                principalColumn: "AddressID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeUsers_Addresses_AddressID",
                table: "EmployeeUsers");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeUsers_AddressID",
                table: "EmployeeUsers");

            migrationBuilder.DropColumn(
                name: "AddressID",
                table: "EmployeeUsers");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "EmployeeUsers",
                type: "text",
                nullable: true);
        }
    }
}
