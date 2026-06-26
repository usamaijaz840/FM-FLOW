using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveProjectTypeToEstimateType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add the new EstimateTypeId column to RequestedEstimates
            migrationBuilder.AddColumn<int>(
                name: "EstimateTypeId",
                table: "RequestedEstimates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Step 2: Copy ProjectTypeID from Projects to EstimateTypeId in RequestedEstimates
            migrationBuilder.Sql(@"
                UPDATE ""RequestedEstimates"" re
                SET ""EstimateTypeId"" = p.""ProjectTypeID""
                FROM ""Projects"" p
                WHERE re.""ProjectID"" = p.""ProjectID""
            ");

            // Step 3: Drop the foreign key and index from Projects
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_EstimateTypes_ProjectTypeID",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectTypeID",
                table: "Projects");

            // Step 4: Drop the ProjectTypeID column from Projects
            migrationBuilder.DropColumn(
                name: "ProjectTypeID",
                table: "Projects");

            // Step 5: Create index and foreign key for RequestedEstimates
            migrationBuilder.CreateIndex(
                name: "IX_RequestedEstimates_EstimateTypeId",
                table: "RequestedEstimates",
                column: "EstimateTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestedEstimates_EstimateTypes_EstimateTypeId",
                table: "RequestedEstimates",
                column: "EstimateTypeId",
                principalTable: "EstimateTypes",
                principalColumn: "EstimateTypeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop foreign key and index from RequestedEstimates
            migrationBuilder.DropForeignKey(
                name: "FK_RequestedEstimates_EstimateTypes_EstimateTypeId",
                table: "RequestedEstimates");

            migrationBuilder.DropIndex(
                name: "IX_RequestedEstimates_EstimateTypeId",
                table: "RequestedEstimates");

            // Step 2: Add ProjectTypeID column back to Projects
            migrationBuilder.AddColumn<int>(
                name: "ProjectTypeID",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Step 3: Copy EstimateTypeId from RequestedEstimates back to Projects
            // Get the first RequestedEstimate's EstimateTypeId for each Project
            migrationBuilder.Sql(@"
                UPDATE ""Projects"" p
                SET ""ProjectTypeID"" = (
                    SELECT re.""EstimateTypeId""
                    FROM ""RequestedEstimates"" re
                    WHERE re.""ProjectID"" = p.""ProjectID""
                    LIMIT 1
                )
            ");

            // Step 4: Drop the EstimateTypeId column from RequestedEstimates
            migrationBuilder.DropColumn(
                name: "EstimateTypeId",
                table: "RequestedEstimates");

            // Step 5: Recreate index and foreign key for Projects
            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectTypeID",
                table: "Projects",
                column: "ProjectTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_EstimateTypes_ProjectTypeID",
                table: "Projects",
                column: "ProjectTypeID",
                principalTable: "EstimateTypes",
                principalColumn: "EstimateTypeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
