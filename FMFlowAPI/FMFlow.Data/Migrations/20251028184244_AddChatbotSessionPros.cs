using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatbotSessionPros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatBotSessionPros",
                columns: table => new
                {
                    ChatBotSessionProId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExpireDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    ProId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatBotSessionPros", x => x.ChatBotSessionProId);
                    table.ForeignKey(
                        name: "FK_ChatBotSessionPros_FMFlowUsers_ProId",
                        column: x => x.ProId,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatBotSessionPros_ProId",
                table: "ChatBotSessionPros",
                column: "ProId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatBotSessionPros_SessionId_ProId",
                table: "ChatBotSessionPros",
                columns: new[] { "SessionId", "ProId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatBotSessionPros");
        }
    }
}
