using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequestedEstimatesRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Estimates");

            migrationBuilder.CreateTable(
                name: "RequestedEstimates",
                columns: table => new
                {
                    RequestedEstimateID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectID = table.Column<int>(type: "integer", nullable: false),
                    ProUserID = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestedEstimates", x => x.RequestedEstimateID);
                    table.ForeignKey(
                        name: "FK_RequestedEstimates_FMFlowUsers_ProUserID",
                        column: x => x.ProUserID,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_RequestedEstimates_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestedEstimates_ProjectID",
                table: "RequestedEstimates",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestedEstimates_ProUserID",
                table: "RequestedEstimates",
                column: "ProUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestedEstimates");

            migrationBuilder.CreateTable(
                name: "Estimates",
                columns: table => new
                {
                    EstimateID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectID = table.Column<int>(type: "integer", nullable: false),
                    ProUserID = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estimates", x => x.EstimateID);
                    table.ForeignKey(
                        name: "FK_Estimates_FMFlowUsers_ProUserID",
                        column: x => x.ProUserID,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Estimates_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_ProjectID",
                table: "Estimates",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_ProUserID",
                table: "Estimates",
                column: "ProUserID");
        }
    }
}
