using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrateProUserDetailsBusinessAddressFieldsToAddressEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add the new BusinessAddressID column as nullable
            migrationBuilder.AddColumn<int>(
                name: "BusinessAddressID",
                table: "ProUserDetails",
                type: "integer",
                nullable: true);

            // Step 2: Migrate existing business address data to the Addresses table
            // Create one address record per ProUserDetails to ensure each Pro has their own address
            migrationBuilder.Sql("""
                CREATE UNLOGGED TABLE
                    temp_pro_addresses_20251119171949
                        ("AddressId" INTEGER NOT NULL DEFAULT NEXTVAL('"Addresses_AddressID_seq"')
                        ,"Line1" TEXT NOT NULL DEFAULT ''
                        ,"Line2" TEXT NOT NULL DEFAULT ''
                        ,"StateId" TEXT NOT NULL DEFAULT ''
                        ,"City" TEXT NOT NULL DEFAULT ''
                        ,"ZipCode" TEXT NOT NULL DEFAULT ''
                        ,"ProId" INTEGER NOT NULL);
                
                    INSERT INTO
                    temp_pro_addresses_20251119171949
                        ("Line1"
                        ,"StateId"
                        ,"City"
                        ,"ZipCode"
                        ,"ProId")
                    SELECT
                        "BusinessAddress"
                        ,"State"
                        ,"City"
                        ,"ZipCode"
                        ,"UserID"
                    FROM
                        "ProUserDetails";
                
                    INSERT INTO
                        "Addresses"
                            ("AddressID"
                            ,"Line1"
                            ,"StateId"
                            ,"City"
                            ,"ZipCode")
                    SELECT
                        "AddressId"
                        ,"Line1"
                        ,"StateId"
                        ,"City"
                        ,"ZipCode"
                    FROM
                    temp_pro_addresses_20251119171949;
                
                    UPDATE
                        "ProUserDetails"
                    SET
                        "BusinessAddressID" = tpa."AddressId"
                    FROM
                        temp_pro_addresses_20251119171949 tpa
                    WHERE
                        "ProUserDetails"."UserID" = tpa."ProId";
                
                    DROP TABLE
                    temp_pro_addresses_20251119171949;
                """);

            // Step 3: Drop the old columns
            migrationBuilder.DropColumn(
                name: "BusinessAddress",
                table: "ProUserDetails");

            migrationBuilder.DropColumn(
                name: "City",
                table: "ProUserDetails");

            migrationBuilder.DropColumn(
                name: "State",
                table: "ProUserDetails");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "ProUserDetails");

            // Step 4: Add index and foreign key
            migrationBuilder.CreateIndex(
                name: "IX_ProUserDetails_BusinessAddressID",
                table: "ProUserDetails",
                column: "BusinessAddressID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserDetails_Addresses_BusinessAddressID",
                table: "ProUserDetails",
                column: "BusinessAddressID",
                principalTable: "Addresses",
                principalColumn: "AddressID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_ProUserDetails_Addresses_BusinessAddressID",
                table: "ProUserDetails");

            migrationBuilder.DropIndex(
                name: "IX_ProUserDetails_BusinessAddressID",
                table: "ProUserDetails");

            // Step 2: Add back the old columns
            migrationBuilder.AddColumn<string>(
                name: "BusinessAddress",
                table: "ProUserDetails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "ProUserDetails",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "ProUserDetails",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "ProUserDetails",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Step 3: Migrate data back from Addresses table to ProUserDetails
            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails""
                SET 
                    ""BusinessAddress"" = a.""Line1"",
                    ""City"" = a.""City"",
                    ""State"" = a.""StateId"",
                    ""ZipCode"" = a.""ZipCode""
                FROM ""Addresses"" a
                WHERE ""ProUserDetails"".""BusinessAddressID"" = a.""AddressID"";
            ");

            // Step 4: Drop the BusinessAddressID column
            migrationBuilder.DropColumn(
                name: "BusinessAddressID",
                table: "ProUserDetails");

            // Note: The Addresses records created during Up migration will remain in the database
            // You may want to manually clean them up if needed
        }
    }
}
