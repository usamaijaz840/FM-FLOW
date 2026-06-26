using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameLeadTimelineColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "EventName",
				table: "LeadTimelines",
				newName: "EventNameKey");

			migrationBuilder.AddColumn<string>(
				name: "EventKey",
				table: "LeadTimelines",
				type: "text",
				nullable: false,
				defaultValue: "");

			migrationBuilder.AddColumn<string>(
			   name: "EventParameters",
			   table: "LeadTimelines",
			   type: "text",
			   nullable: false,
			   defaultValue: "");

			migrationBuilder.DropColumn(
				name: "Description",
				table: "LeadTimelines");
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "EventKey",
				table: "LeadTimelines");

			migrationBuilder.DropColumn(
				name: "EventParameters",
				table: "LeadTimelines");

			migrationBuilder.DropColumn(
				name: "EventKey",
				table: "LeadTimelines");

			migrationBuilder.AddColumn<string>(
				name: "Description",
				table: "LeadTimelines",
				type: "text",
				nullable: false,
				defaultValue: "");
		}
    }
}
