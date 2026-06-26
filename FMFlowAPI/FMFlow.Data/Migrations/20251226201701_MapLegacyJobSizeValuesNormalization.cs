using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class MapLegacyJobSizeValuesNormalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add GIN index to accelerate array containment/overlap queries on SizeOfJob
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_pro_sizeofjob_gin 
                ON ""ProUserDetails"" USING gin (""SizeOfJob"");
            ");

            // Normalize any brace-wrapped single-element arrays produced by earlier casts
            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = ARRAY[regexp_replace(""SizeOfJob""[1], '^{|}$', '', 'g')]::text[]
                WHERE cardinality(""SizeOfJob"") = 1 AND ""SizeOfJob""[1] ~ '^{.*}$';
            ");

            // Split single-element arrays containing comma-delimited strings into proper arrays
            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = regexp_split_to_array(""SizeOfJob""[1], ',')::text[]
                WHERE cardinality(""SizeOfJob"") = 1 AND position(',' in ""SizeOfJob""[1]) > 0;
            ");

            // Case-insensitive mapping of legacy labels to new monetary range buckets
            // Any -> all buckets
            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = ARRAY['under_500','500_1500','1500_5000','5000_10000','over_10000']::text[]
                WHERE cardinality(""SizeOfJob"") = 1 AND lower(trim(""SizeOfJob""[1])) = 'any';
            ");

            // Small -> under_500
            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = ARRAY['under_500']::text[]
                WHERE cardinality(""SizeOfJob"") = 1 AND lower(trim(""SizeOfJob""[1])) = 'small';
            ");

            // Medium -> 500_1500
            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = ARRAY['500_1500']::text[]
                WHERE cardinality(""SizeOfJob"") = 1 AND lower(trim(""SizeOfJob""[1])) = 'medium';
            ");

            // Large -> 1500_5000, 5000_10000, over_10000
            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = ARRAY['1500_5000','5000_10000','over_10000']::text[]
                WHERE cardinality(""SizeOfJob"") = 1 AND lower(trim(""SizeOfJob""[1])) = 'large';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop GIN index if present
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS idx_pro_sizeofjob_gin;
            ");

            // Reversible mapping back to legacy labels (exact matches only)
            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = ARRAY['Any']::text[]
                WHERE ""SizeOfJob"" = ARRAY['under_500','500_1500','1500_5000','5000_10000','over_10000']::text[];
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = ARRAY['Small']::text[]
                WHERE ""SizeOfJob"" = ARRAY['under_500']::text[];
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = ARRAY['Medium']::text[]
                WHERE ""SizeOfJob"" = ARRAY['500_1500']::text[];
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ProUserDetails"" 
                SET ""SizeOfJob"" = ARRAY['Large']::text[]
                WHERE ""SizeOfJob"" = ARRAY['1500_5000','5000_10000','over_10000']::text[];
            ");
        }
    }
}
