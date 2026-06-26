using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class EstimatesAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estimates",
                columns: table => new
                {
                    EstimateID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestedEstimateID = table.Column<int>(type: "integer", nullable: false),
                    ProUserID = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attributes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estimates", x => x.EstimateID);
                    table.ForeignKey(
                        name: "FK_Estimates_FMFlowUsers_ProUserID",
                        column: x => x.ProUserID,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Estimates_RequestedEstimates_RequestedEstimateID",
                        column: x => x.RequestedEstimateID,
                        principalTable: "RequestedEstimates",
                        principalColumn: "RequestedEstimateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_ProUserID",
                table: "Estimates",
                column: "ProUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_RequestedEstimateID",
                table: "Estimates",
                column: "RequestedEstimateID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Estimates");
        }
    }
}
