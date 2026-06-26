using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
	/// <inheritdoc />
	public partial class JsonDocumentAttributesAndEnumsAsStrings : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<string>(
				name: "Status",
				table: "Projects",
				type: "text",
				nullable: false,
				oldClrType: typeof(int),
				oldType: "integer");

			migrationBuilder.AlterColumn<string>(
				name: "CustomerType",
				table: "Leads",
				type: "text",
				nullable: false,
				oldClrType: typeof(int),
				oldType: "integer");

			migrationBuilder.AlterColumn<string>(
				name: "Status",
				table: "Estimates",
				type: "text",
				nullable: false,
				oldClrType: typeof(int),
				oldType: "integer");

			migrationBuilder.AlterColumn<string>(
				name: "BillingFrequency",
				table: "BillingPlans",
				type: "text",
				nullable: false,
				oldClrType: typeof(int),
				oldType: "integer");

			migrationBuilder.Sql("""
    ALTER TABLE "Estimates"
    ALTER COLUMN "Attributes"
    TYPE jsonb
    USING "Attributes"::jsonb;
""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<int>(
				name: "Status",
				table: "Projects",
				type: "integer",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "text");

			migrationBuilder.AlterColumn<int>(
				name: "CustomerType",
				table: "Leads",
				type: "integer",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "text");

			migrationBuilder.AlterColumn<int>(
				name: "Status",
				table: "Estimates",
				type: "integer",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "text");

			migrationBuilder.AlterColumn<string>(
				name: "Attributes",
				table: "Estimates",
				type: "text",
				nullable: true,
				oldClrType: typeof(JsonDocument),
				oldType: "jsonb",
				oldNullable: true);

			migrationBuilder.AlterColumn<int>(
				name: "BillingFrequency",
				table: "BillingPlans",
				type: "integer",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "text");
		}
	}
}
