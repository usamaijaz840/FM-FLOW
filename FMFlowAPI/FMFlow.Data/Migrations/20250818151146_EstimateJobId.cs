using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class EstimateJobId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Estimates_EstimateId",
                table: "Jobs");

            migrationBuilder.AddColumn<int>(
                name: "JobId",
                table: "Estimates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Estimates_EstimateId",
                table: "Jobs",
                column: "EstimateId",
                principalTable: "Estimates",
                principalColumn: "EstimateID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Estimates_EstimateId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Estimates");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Estimates_EstimateId",
                table: "Jobs",
                column: "EstimateId",
                principalTable: "Estimates",
                principalColumn: "EstimateID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
