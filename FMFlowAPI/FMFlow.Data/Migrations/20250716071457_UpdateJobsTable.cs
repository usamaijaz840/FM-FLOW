using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJobsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Jobs",
                newName: "SignOffDate");

            migrationBuilder.RenameColumn(
                name: "FinishDate",
                table: "Jobs",
                newName: "ActualDateWorkStarted");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActualDateWorkCompleted",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CleanedUpWorkAreas",
                table: "Jobs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CompletedScopeOfWork",
                table: "Jobs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContractorWorkIsSatisfactory",
                table: "Jobs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CooperativeWithCustomer",
                table: "Jobs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifiedPriorToArrival",
                table: "Jobs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RateContractorPerformance",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledDateWorkCompleted",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledDateWorkStarted",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "SignOffComment",
                table: "Jobs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualDateWorkCompleted",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CleanedUpWorkAreas",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CompletedScopeOfWork",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ContractorWorkIsSatisfactory",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CooperativeWithCustomer",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "NotifiedPriorToArrival",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RateContractorPerformance",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ScheduledDateWorkCompleted",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ScheduledDateWorkStarted",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SignOffComment",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "SignOffDate",
                table: "Jobs",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "ActualDateWorkStarted",
                table: "Jobs",
                newName: "FinishDate");
        }
    }
}
