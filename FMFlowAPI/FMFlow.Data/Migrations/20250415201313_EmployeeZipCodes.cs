using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeZipCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProUserDetail_EmployeeUsers_AccountManagerUserID",
                table: "ProUserDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserToProZipcode_ProZipcode_Zipcode",
                table: "ProUserToProZipcode");

            migrationBuilder.DropTable(
                name: "ProZipcode");

            migrationBuilder.DropIndex(
                name: "IX_ProUserDetail_AccountManagerUserID",
                table: "ProUserDetail");

            migrationBuilder.DropColumn(
                name: "AccountManagerUserID",
                table: "ProUserDetail");

            migrationBuilder.CreateTable(
                name: "ZipCodes",
                columns: table => new
                {
                    Zipcode = table.Column<string>(type: "text", nullable: false),
                    StateAbbreviation = table.Column<string>(type: "text", nullable: false),
                    County = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZipCodes", x => x.Zipcode);
                    table.ForeignKey(
                        name: "FK_ZipCodes_State_StateAbbreviation",
                        column: x => x.StateAbbreviation,
                        principalTable: "State",
                        principalColumn: "Abbreviation",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeUserZipCode",
                columns: table => new
                {
                    AssignedZipCodesZipcode = table.Column<string>(type: "text", nullable: false),
                    EmployeesAssignedUserID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeUserZipCode", x => new { x.AssignedZipCodesZipcode, x.EmployeesAssignedUserID });
                    table.ForeignKey(
                        name: "FK_EmployeeUserZipCode_EmployeeUsers_EmployeesAssignedUserID",
                        column: x => x.EmployeesAssignedUserID,
                        principalTable: "EmployeeUsers",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeUserZipCode_ZipCodes_AssignedZipCodesZipcode",
                        column: x => x.AssignedZipCodesZipcode,
                        principalTable: "ZipCodes",
                        principalColumn: "Zipcode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeUserZipCode_EmployeesAssignedUserID",
                table: "EmployeeUserZipCode",
                column: "EmployeesAssignedUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ZipCodes_StateAbbreviation",
                table: "ZipCodes",
                column: "StateAbbreviation");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserToProZipcode_ZipCodes_Zipcode",
                table: "ProUserToProZipcode",
                column: "Zipcode",
                principalTable: "ZipCodes",
                principalColumn: "Zipcode",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProUserToProZipcode_ZipCodes_Zipcode",
                table: "ProUserToProZipcode");

            migrationBuilder.DropTable(
                name: "EmployeeUserZipCode");

            migrationBuilder.DropTable(
                name: "ZipCodes");

            migrationBuilder.AddColumn<int>(
                name: "AccountManagerUserID",
                table: "ProUserDetail",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProZipcode",
                columns: table => new
                {
                    Zipcode = table.Column<string>(type: "text", nullable: false),
                    StateAbbreviation = table.Column<string>(type: "text", nullable: false),
                    County = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProZipcode", x => x.Zipcode);
                    table.ForeignKey(
                        name: "FK_ProZipcode_State_StateAbbreviation",
                        column: x => x.StateAbbreviation,
                        principalTable: "State",
                        principalColumn: "Abbreviation",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProUserDetail_AccountManagerUserID",
                table: "ProUserDetail",
                column: "AccountManagerUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ProZipcode_StateAbbreviation",
                table: "ProZipcode",
                column: "StateAbbreviation");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserDetail_EmployeeUsers_AccountManagerUserID",
                table: "ProUserDetail",
                column: "AccountManagerUserID",
                principalTable: "EmployeeUsers",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserToProZipcode_ProZipcode_Zipcode",
                table: "ProUserToProZipcode",
                column: "Zipcode",
                principalTable: "ProZipcode",
                principalColumn: "Zipcode",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
