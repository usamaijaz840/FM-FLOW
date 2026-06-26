using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrateProSherwinHomeAddressFieldsToAddressRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add the new foreign key column as nullable initially
            migrationBuilder.AddColumn<int>(
                name: "SherwinHomeAddressID",
                table: "ProUserDetails",
                type: "integer",
                nullable: true);

            // Step 2: Migrate data using an UNLOGGED temp table and the Addresses sequence
            // 1) Create an UNLOGGED temp table to stage new addresses (faster; clearly throwaway)
            // 2) Insert staged rows pulling new AddressIDs from the Addresses sequence
            // 3) Insert staged addresses into Addresses using the pre-assigned AddressIDs
            // 4) Update ProUserDetails to reference the new SherwinHomeAddressID
            // 5) Drop the temp table
            migrationBuilder.Sql("""
                CREATE UNLOGGED TABLE
                    temp_pro_store_addresses_20251119215149
                        ("AddressId" INTEGER NOT NULL DEFAULT NEXTVAL('"Addresses_AddressID_seq"')
                        ,"Line1" TEXT NOT NULL DEFAULT ''
                        ,"Line2" TEXT NOT NULL DEFAULT ''
                        ,"StateId" TEXT NOT NULL DEFAULT ''
                        ,"City" TEXT NOT NULL DEFAULT ''
                        ,"ZipCode" TEXT NOT NULL DEFAULT ''
                        ,"ProId" INTEGER NOT NULL);

                INSERT INTO
                    temp_pro_store_addresses_20251119215149
                        ("Line1"
                        ,"StateId"
                        ,"ZipCode"
                        ,"ProId")
                SELECT
                     p."AddressOfStore"
                    ,ba."StateId"
                    ,p."ZipCodeOfStore"
                    ,p."UserID"
                FROM
                    "ProUserDetails" p
                LEFT JOIN "Addresses" ba ON p."BusinessAddressID" = ba."AddressID"
                WHERE p."AddressOfStore" IS NOT NULL OR p."ZipCodeOfStore" IS NOT NULL;

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
                    temp_pro_store_addresses_20251119215149;

                UPDATE
                    "ProUserDetails"
                SET
                    "SherwinHomeAddressID" = tpsa."AddressId"
                FROM
                    temp_pro_store_addresses_20251119215149 tpsa
                WHERE
                    "ProUserDetails"."UserID" = tpsa."ProId";

                DROP TABLE
                    temp_pro_store_addresses_20251119215149;
            """);

            // Step 3: Drop the old columns
            migrationBuilder.DropColumn(
                name: "AddressOfStore",
                table: "ProUserDetails");

            migrationBuilder.DropColumn(
                name: "ZipCodeOfStore",
                table: "ProUserDetails");

            // Step 4: Add index and foreign key constraint
            migrationBuilder.CreateIndex(
                name: "IX_ProUserDetails_SherwinHomeAddressID",
                table: "ProUserDetails",
                column: "SherwinHomeAddressID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProUserDetails_Addresses_SherwinHomeAddressID",
                table: "ProUserDetails",
                column: "SherwinHomeAddressID",
                principalTable: "Addresses",
                principalColumn: "AddressID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add back the old columns
            migrationBuilder.AddColumn<string>(
                name: "AddressOfStore",
                table: "ProUserDetails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCodeOfStore",
                table: "ProUserDetails",
                type: "text",
                nullable: true);

            // Step 2: Restore data from Address records back to flat columns
            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" p
                SET 
                    ""AddressOfStore"" = a.""Line1"",
                    ""ZipCodeOfStore"" = a.""ZipCode""
                FROM ""Addresses"" a
                WHERE p.""SherwinHomeAddressID"" = a.""AddressID"";
            ");

            // Step 3: Drop the foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_ProUserDetails_Addresses_SherwinHomeAddressID",
                table: "ProUserDetails");

            migrationBuilder.DropIndex(
                name: "IX_ProUserDetails_SherwinHomeAddressID",
                table: "ProUserDetails");

            // Step 4: Drop the foreign key column
            migrationBuilder.DropColumn(
                name: "SherwinHomeAddressID",
                table: "ProUserDetails");

            // Note: The Address records created during Up() are NOT automatically deleted
            // in Down() to prevent data loss. Manual cleanup may be needed if required.
        }
    }
}
