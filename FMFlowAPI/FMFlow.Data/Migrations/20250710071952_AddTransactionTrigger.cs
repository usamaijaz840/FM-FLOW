using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
				CREATE OR REPLACE FUNCTION update_estimate_after_transaction()
				RETURNS TRIGGER AS $$
				BEGIN
					UPDATE ""Estimates""
					SET 
						""PaidAmount"" = sums.""TotalCredit"",
						""HasBeenPaid"" = (
							CASE
								WHEN sums.""TotalCredit"" >= sums.""TotalDebit""
								THEN TRUE
								ELSE FALSE
							END
						)
					FROM (
						WITH sums AS (
							SELECT 
								""EstimateId"",
								COALESCE(SUM(""Credit""), 0) AS ""TotalCredit"",
								COALESCE(SUM(""Debit""), 0) AS ""TotalDebit""
							FROM ""Transactions""
							GROUP BY ""EstimateId""
						)
						SELECT * FROM sums WHERE ""EstimateId"" = NEW.""EstimateId""
					) AS sums
					WHERE ""Estimates"".""EstimateID"" = NEW.""EstimateId"";

					RETURN NEW;
				END;
				$$ LANGUAGE plpgsql;

				CREATE TRIGGER trg_update_estimate_on_transaction_insert
				AFTER INSERT ON ""Transactions""
				FOR EACH ROW
				EXECUTE FUNCTION update_estimate_after_transaction();
			");
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
				DROP TRIGGER IF EXISTS trg_update_estimate_on_transaction_insert ON ""Transactions"";
				DROP FUNCTION IF EXISTS update_estimate_after_transaction;
			");
		}
    }
}
