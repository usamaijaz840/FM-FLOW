using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class EstimateJobIdBidirectional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Estimates_EstimateId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_EstimateId",
                table: "Jobs");

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_JobId",
                table: "Estimates",
                column: "JobId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Estimates_Jobs_JobId",
                table: "Estimates",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Estimates_Jobs_JobId",
                table: "Estimates");

            migrationBuilder.DropIndex(
                name: "IX_Estimates_JobId",
                table: "Estimates");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_EstimateId",
                table: "Jobs",
                column: "EstimateId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Estimates_EstimateId",
                table: "Jobs",
                column: "EstimateId",
                principalTable: "Estimates",
                principalColumn: "EstimateID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
