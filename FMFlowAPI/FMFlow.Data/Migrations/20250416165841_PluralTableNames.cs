using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class PluralTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_State_StateId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeUsers_FMFlowUser_UserID",
                table: "EmployeeUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Leads_LeadSource_LeadSourceID",
                table: "Leads");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserDetail_Billing_BillingID",
                table: "ProUserDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserDetail_FMFlowUser_UserID",
                table: "ProUserDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserDetail_TimeZones_FMTimeZoneID",
                table: "ProUserDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserToProZipcode_ProUserDetail_UserID",
                table: "ProUserToProZipcode");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserToProZipcode_ZipCodes_Zipcode",
                table: "ProUserToProZipcode");

            migrationBuilder.DropForeignKey(
                name: "FK_ZipCodes_State_StateAbbreviation",
                table: "ZipCodes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TimeZones",
                table: "TimeZones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_State",
                table: "State");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sheen",
                table: "Sheen");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProUserToProZipcode",
                table: "ProUserToProZipcode");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProUserDetail",
                table: "ProUserDetail");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Product",
                table: "Product");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadSource",
                table: "LeadSource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FMFlowUser",
                table: "FMFlowUser");

            migrationBuilder.RenameTable(
                name: "TimeZones",
                newName: "FMTimeZones");

            migrationBuilder.RenameTable(
                name: "State",
                newName: "States");

            migrationBuilder.RenameTable(
                name: "Sheen",
                newName: "Sheens");

            migrationBuilder.RenameTable(
                name: "ProUserToProZipcode",
                newName: "ProUserToProZipcodes");

            migrationBuilder.RenameTable(
                name: "ProUserDetail",
                newName: "ProUserDetails");

            migrationBuilder.RenameTable(
                name: "Product",
                newName: "Products");

            migrationBuilder.RenameTable(
                name: "LeadSource",
                newName: "LeadSources");

            migrationBuilder.RenameTable(
                name: "FMFlowUser",
                newName: "FMFlowUsers");

            migrationBuilder.RenameIndex(
                name: "IX_ProUserToProZipcode_UserID",
                table: "ProUserToProZipcodes",
                newName: "IX_ProUserToProZipcodes_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_ProUserDetail_FMTimeZoneID",
                table: "ProUserDetails",
                newName: "IX_ProUserDetails_FMTimeZoneID");

            migrationBuilder.RenameIndex(
                name: "IX_ProUserDetail_BillingID",
                table: "ProUserDetails",
                newName: "IX_ProUserDetails_BillingID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FMTimeZones",
                table: "FMTimeZones",
                column: "TimeZoneId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_States",
                table: "States",
                column: "Abbreviation");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sheens",
                table: "Sheens",
                column: "SheenID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProUserToProZipcodes",
                table: "ProUserToProZipcodes",
                columns: new[] { "Zipcode", "UserID" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProUserDetails",
                table: "ProUserDetails",
                column: "UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "ProductID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadSources",
                table: "LeadSources",
                column: "LeadSourceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FMFlowUsers",
                table: "FMFlowUsers",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_States_StateId",
                table: "Addresses",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Abbreviation",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeUsers_FMFlowUsers_UserID",
                table: "EmployeeUsers",
                column: "UserID",
                principalTable: "FMFlowUsers",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_LeadSources_LeadSourceID",
                table: "Leads",
                column: "LeadSourceID",
                principalTable: "LeadSources",
                principalColumn: "LeadSourceID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserDetails_Billing_BillingID",
                table: "ProUserDetails",
                column: "BillingID",
                principalTable: "Billing",
                principalColumn: "BillingID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserDetails_FMFlowUsers_UserID",
                table: "ProUserDetails",
                column: "UserID",
                principalTable: "FMFlowUsers",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserDetails_FMTimeZones_FMTimeZoneID",
                table: "ProUserDetails",
                column: "FMTimeZoneID",
                principalTable: "FMTimeZones",
                principalColumn: "TimeZoneId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserToProZipcodes_ProUserDetails_UserID",
                table: "ProUserToProZipcodes",
                column: "UserID",
                principalTable: "ProUserDetails",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserToProZipcodes_ZipCodes_Zipcode",
                table: "ProUserToProZipcodes",
                column: "Zipcode",
                principalTable: "ZipCodes",
                principalColumn: "Zipcode",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ZipCodes_States_StateAbbreviation",
                table: "ZipCodes",
                column: "StateAbbreviation",
                principalTable: "States",
                principalColumn: "Abbreviation",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_States_StateId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeUsers_FMFlowUsers_UserID",
                table: "EmployeeUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Leads_LeadSources_LeadSourceID",
                table: "Leads");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserDetails_Billing_BillingID",
                table: "ProUserDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserDetails_FMFlowUsers_UserID",
                table: "ProUserDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserDetails_FMTimeZones_FMTimeZoneID",
                table: "ProUserDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserToProZipcodes_ProUserDetails_UserID",
                table: "ProUserToProZipcodes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProUserToProZipcodes_ZipCodes_Zipcode",
                table: "ProUserToProZipcodes");

            migrationBuilder.DropForeignKey(
                name: "FK_ZipCodes_States_StateAbbreviation",
                table: "ZipCodes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_States",
                table: "States");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sheens",
                table: "Sheens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProUserToProZipcodes",
                table: "ProUserToProZipcodes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProUserDetails",
                table: "ProUserDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadSources",
                table: "LeadSources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FMTimeZones",
                table: "FMTimeZones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FMFlowUsers",
                table: "FMFlowUsers");

            migrationBuilder.RenameTable(
                name: "States",
                newName: "State");

            migrationBuilder.RenameTable(
                name: "Sheens",
                newName: "Sheen");

            migrationBuilder.RenameTable(
                name: "ProUserToProZipcodes",
                newName: "ProUserToProZipcode");

            migrationBuilder.RenameTable(
                name: "ProUserDetails",
                newName: "ProUserDetail");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "Product");

            migrationBuilder.RenameTable(
                name: "LeadSources",
                newName: "LeadSource");

            migrationBuilder.RenameTable(
                name: "FMTimeZones",
                newName: "TimeZones");

            migrationBuilder.RenameTable(
                name: "FMFlowUsers",
                newName: "FMFlowUser");

            migrationBuilder.RenameIndex(
                name: "IX_ProUserToProZipcodes_UserID",
                table: "ProUserToProZipcode",
                newName: "IX_ProUserToProZipcode_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_ProUserDetails_FMTimeZoneID",
                table: "ProUserDetail",
                newName: "IX_ProUserDetail_FMTimeZoneID");

            migrationBuilder.RenameIndex(
                name: "IX_ProUserDetails_BillingID",
                table: "ProUserDetail",
                newName: "IX_ProUserDetail_BillingID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_State",
                table: "State",
                column: "Abbreviation");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sheen",
                table: "Sheen",
                column: "SheenID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProUserToProZipcode",
                table: "ProUserToProZipcode",
                columns: new[] { "Zipcode", "UserID" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProUserDetail",
                table: "ProUserDetail",
                column: "UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Product",
                table: "Product",
                column: "ProductID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadSource",
                table: "LeadSource",
                column: "LeadSourceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TimeZones",
                table: "TimeZones",
                column: "TimeZoneId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FMFlowUser",
                table: "FMFlowUser",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_State_StateId",
                table: "Addresses",
                column: "StateId",
                principalTable: "State",
                principalColumn: "Abbreviation",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeUsers_FMFlowUser_UserID",
                table: "EmployeeUsers",
                column: "UserID",
                principalTable: "FMFlowUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_LeadSource_LeadSourceID",
                table: "Leads",
                column: "LeadSourceID",
                principalTable: "LeadSource",
                principalColumn: "LeadSourceID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserDetail_Billing_BillingID",
                table: "ProUserDetail",
                column: "BillingID",
                principalTable: "Billing",
                principalColumn: "BillingID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserDetail_FMFlowUser_UserID",
                table: "ProUserDetail",
                column: "UserID",
                principalTable: "FMFlowUser",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserDetail_TimeZones_FMTimeZoneID",
                table: "ProUserDetail",
                column: "FMTimeZoneID",
                principalTable: "TimeZones",
                principalColumn: "TimeZoneId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserToProZipcode_ProUserDetail_UserID",
                table: "ProUserToProZipcode",
                column: "UserID",
                principalTable: "ProUserDetail",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserToProZipcode_ZipCodes_Zipcode",
                table: "ProUserToProZipcode",
                column: "Zipcode",
                principalTable: "ZipCodes",
                principalColumn: "Zipcode",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ZipCodes_State_StateAbbreviation",
                table: "ZipCodes",
                column: "StateAbbreviation",
                principalTable: "State",
                principalColumn: "Abbreviation",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
