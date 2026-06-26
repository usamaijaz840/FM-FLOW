using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertSizeOfJobColumnToTextArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stage into a temporary array column to avoid malformed literal issues
            migrationBuilder.Sql(@"
                ALTER TABLE ""ProUserDetails"" 
                ADD COLUMN ""SizeOfJob_temp"" text[];
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob_temp"" = CASE 
                    WHEN ""SizeOfJob"" IS NULL OR ""SizeOfJob"" = '' THEN NULL
                    ELSE ARRAY[""SizeOfJob""]::text[]
                END;
            ");

            migrationBuilder.DropColumn(
                name: "SizeOfJob",
                table: "ProUserDetails");

            migrationBuilder.RenameColumn(
                name: "SizeOfJob_temp",
                table: "ProUserDetails",
                newName: "SizeOfJob");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Stage back to text
            migrationBuilder.Sql(@"
                ALTER TABLE ""ProUserDetails"" 
                ADD COLUMN ""SizeOfJob_temp"" text;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob_temp"" = CASE 
                    WHEN ""SizeOfJob"" IS NULL THEN NULL
                    ELSE ""SizeOfJob""[1]
                END;
            ");

            migrationBuilder.DropColumn(
                name: "SizeOfJob",
                table: "ProUserDetails");

            migrationBuilder.RenameColumn(
                name: "SizeOfJob_temp",
                table: "ProUserDetails",
                newName: "SizeOfJob");
        }
    }
}
