using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class EventsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			// Safely update type of ProWeekDayAvailabilities.StartTime from string to TimeOnly
			migrationBuilder.RenameColumn("StartTime", "ProWeekDayAvailabilities", "StartTimeOld");

			migrationBuilder.AddColumn<TimeOnly>(
				name: "StartTime",
				table: "ProWeekDayAvailabilities",
				type: "time without time zone",
				nullable: true);

			migrationBuilder.Sql("""UPDATE "ProWeekDayAvailabilities" SET "StartTime" = "StartTimeOld"::time without time zone;""");

			migrationBuilder.DropColumn(
				name: "StartTimeOld",
				table: "ProWeekDayAvailabilities");

			migrationBuilder.AlterColumn<TimeOnly>(
				name: "StartTime",
				table: "ProWeekDayAvailabilities",
				type: "time without time zone",
				nullable: false);

			// Safely update type of ProWeekDayAvailabilities.EndTime from string to TimeOnly
			migrationBuilder.RenameColumn("EndTime", "ProWeekDayAvailabilities", "EndTimeOld");

			migrationBuilder.AddColumn<TimeOnly>(
				name: "EndTime",
				table: "ProWeekDayAvailabilities",
				type: "time without time zone",
				nullable: true);

			migrationBuilder.Sql("""UPDATE "ProWeekDayAvailabilities" SET "EndTime" = "EndTimeOld"::time without time zone;""");

			migrationBuilder.DropColumn(
				name: "EndTimeOld",
				table: "ProWeekDayAvailabilities");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "EndTime",
                table: "ProWeekDayAvailabilities",
                type: "time without time zone",
                nullable: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateAssignedToRsLead",
                table: "ProUserDetails",
                type: "timestamp with time zone",
                nullable: true);

			// Safely update type of Jobs.ScheduledDateWorkStarted from DateTimeOffset to DateOnly
			migrationBuilder.RenameColumn("ScheduledDateWorkStarted", "Jobs", "ScheduledDateWorkStartedOld");

			migrationBuilder.AddColumn<DateOnly>(
				name: "ScheduledDateWorkStarted",
				table: "Jobs",
				type: "date",
				nullable: true);

			migrationBuilder.Sql("""UPDATE "Jobs" SET "ScheduledDateWorkStarted" = "ScheduledDateWorkStartedOld"::date;""");

			migrationBuilder.DropColumn(
				name: "ScheduledDateWorkStartedOld",
				table: "Jobs");

			migrationBuilder.AlterColumn<DateOnly>(
                name: "ScheduledDateWorkStarted",
                table: "Jobs",
                type: "date",
                nullable: false);

			// Safely update type of Jobs.ScheduledDateWorkCompleted from DateTimeOffset to DateOnly
			migrationBuilder.RenameColumn("ScheduledDateWorkCompleted", "Jobs", "ScheduledDateWorkCompletedOld");

			migrationBuilder.AddColumn<DateOnly>(
				name: "ScheduledDateWorkCompleted",
				table: "Jobs",
				type: "date",
				nullable: true);

			migrationBuilder.Sql("""UPDATE "Jobs" SET "ScheduledDateWorkCompleted" = "ScheduledDateWorkCompletedOld"::date;""");

			migrationBuilder.DropColumn(
				name: "ScheduledDateWorkCompletedOld",
				table: "Jobs");

			migrationBuilder.AlterColumn<DateOnly>(
                name: "ScheduledDateWorkCompleted",
                table: "Jobs",
                type: "date",
                nullable: false);

			// Safely update type of Jobs.ActualDateWorkStarted from DateTimeOffset to DateOnly
			migrationBuilder.RenameColumn("ActualDateWorkStarted", "Jobs", "ActualDateWorkStartedOld");

			migrationBuilder.AddColumn<DateOnly>(
				name: "ActualDateWorkStarted",
				table: "Jobs",
				type: "date",
				nullable: true);

			migrationBuilder.Sql("""UPDATE "Jobs" SET "ActualDateWorkStarted" = "ActualDateWorkStartedOld"::date;""");

			migrationBuilder.DropColumn(
				name: "ActualDateWorkStartedOld",
				table: "Jobs");


			// Safely update type of Jobs.ActualDateWorkCompleted from DateTimeOffset to DateOnly
			migrationBuilder.RenameColumn("ActualDateWorkCompleted", "Jobs", "ActualDateWorkCompletedOld");

			migrationBuilder.AddColumn<DateOnly>(
				name: "ActualDateWorkCompleted",
				table: "Jobs",
				type: "date",
				nullable: true);

			migrationBuilder.Sql("""UPDATE "Jobs" SET "ActualDateWorkCompleted" = "ActualDateWorkCompletedOld"::date;""");

			migrationBuilder.DropColumn(
				name: "ActualDateWorkCompletedOld",
				table: "Jobs");

            migrationBuilder.CreateTable(
                name: "CustomerTempPros",
                columns: table => new
                {
                    CustomerTempProId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExpireDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ProId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerTempPros", x => x.CustomerTempProId);
                    table.ForeignKey(
                        name: "FK_CustomerTempPros_FMFlowUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerTempPros_FMFlowUsers_ProId",
                        column: x => x.ProId,
                        principalTable: "FMFlowUsers",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTempPros_CustomerId",
                table: "CustomerTempPros",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTempPros_ProId",
                table: "CustomerTempPros",
                column: "ProId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerTempPros");

            migrationBuilder.DropColumn(
                name: "DateAssignedToRsLead",
                table: "ProUserDetails");

            migrationBuilder.AlterColumn<string>(
                name: "StartTime",
                table: "ProWeekDayAvailabilities",
                type: "text",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "EndTime",
                table: "ProWeekDayAvailabilities",
                type: "text",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ScheduledDateWorkStarted",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ScheduledDateWorkCompleted",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ActualDateWorkStarted",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ActualDateWorkCompleted",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
