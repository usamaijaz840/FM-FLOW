using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledEstimates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Projects",
                newName: "Summary");

            migrationBuilder.AddColumn<int>(
                name: "ScheduledEstimateID",
                table: "Estimates",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScheduledEstimates",
                columns: table => new
                {
                    ScheduledEstimateID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProUserID = table.Column<int>(type: "integer", nullable: false),
                    ProjectID = table.Column<int>(type: "integer", nullable: false),
                    ScheduledDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledEstimates", x => x.ScheduledEstimateID);
                    table.ForeignKey(
                        name: "FK_ScheduledEstimates_FMFlowUsers_ProUserID",
                        column: x => x.ProUserID,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledEstimates_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_ScheduledEstimateID",
                table: "Estimates",
                column: "ScheduledEstimateID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledEstimates_ProjectID",
                table: "ScheduledEstimates",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledEstimates_ProUserID",
                table: "ScheduledEstimates",
                column: "ProUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Estimates_ScheduledEstimates_ScheduledEstimateID",
                table: "Estimates",
                column: "ScheduledEstimateID",
                principalTable: "ScheduledEstimates",
                principalColumn: "ScheduledEstimateID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Estimates_ScheduledEstimates_ScheduledEstimateID",
                table: "Estimates");

            migrationBuilder.DropTable(
                name: "ScheduledEstimates");

            migrationBuilder.DropIndex(
                name: "IX_Estimates_ScheduledEstimateID",
                table: "Estimates");

            migrationBuilder.DropColumn(
                name: "ScheduledEstimateID",
                table: "Estimates");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "Projects",
                newName: "Notes");
        }
    }
}
