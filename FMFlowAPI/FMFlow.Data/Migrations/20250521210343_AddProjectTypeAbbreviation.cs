using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTypeAbbreviation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectTypeAbbreviation",
                table: "ProjectTypes",
                type: "text",
                nullable: true);
                
            migrationBuilder.Sql(@"
                UPDATE ""ProjectTypes"" SET ""ProjectTypeAbbreviation"" = 
                    CASE 
                        WHEN ""ProjectTypeName"" LIKE 'Interior%' THEN 'Int'
                        WHEN ""ProjectTypeName"" LIKE 'Exterior%' THEN 'Ext'
                        ELSE SUBSTRING(""ProjectTypeName"", 1, 3)
                    END
            ");
            
            migrationBuilder.AlterColumn<string>(
                name: "ProjectTypeAbbreviation",
                table: "ProjectTypes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectTypeAbbreviation",
                table: "ProjectTypes");
        }
    }
}
