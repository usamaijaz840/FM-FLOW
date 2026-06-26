using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameProjectTypeTableToEstimateType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectTypes_ProjectTypeID",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectTypes",
                table: "ProjectTypes");

            migrationBuilder.RenameTable(
                name: "ProjectTypes",
                newName: "EstimateTypes");

            migrationBuilder.RenameColumn(
                name: "ProjectTypeName",
                table: "EstimateTypes",
                newName: "EstimateTypeName");

            migrationBuilder.RenameColumn(
                name: "ProjectTypeAbbreviation",
                table: "EstimateTypes",
                newName: "EstimateTypeAbbreviation");

            migrationBuilder.RenameColumn(
                name: "ProjectTypeID",
                table: "EstimateTypes",
                newName: "EstimateTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EstimateTypes",
                table: "EstimateTypes",
                column: "EstimateTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_EstimateTypes_ProjectTypeID",
                table: "Projects",
                column: "ProjectTypeID",
                principalTable: "EstimateTypes",
                principalColumn: "EstimateTypeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_EstimateTypes_ProjectTypeID",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EstimateTypes",
                table: "EstimateTypes");

            migrationBuilder.RenameTable(
                name: "EstimateTypes",
                newName: "ProjectTypes");

            migrationBuilder.RenameColumn(
                name: "EstimateTypeName",
                table: "ProjectTypes",
                newName: "ProjectTypeName");

            migrationBuilder.RenameColumn(
                name: "EstimateTypeAbbreviation",
                table: "ProjectTypes",
                newName: "ProjectTypeAbbreviation");

            migrationBuilder.RenameColumn(
                name: "EstimateTypeId",
                table: "ProjectTypes",
                newName: "ProjectTypeID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectTypes",
                table: "ProjectTypes",
                column: "ProjectTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectTypes_ProjectTypeID",
                table: "Projects",
                column: "ProjectTypeID",
                principalTable: "ProjectTypes",
                principalColumn: "ProjectTypeID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
