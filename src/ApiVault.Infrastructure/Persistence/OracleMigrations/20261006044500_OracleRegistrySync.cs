using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.OracleMigrations
{
    /// <inheritdoc />
    public partial class OracleRegistrySync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing Oracle databases must be upgraded with the guarded script,
            // which backs up rows and reconciles the legacy API_PROJECT IDs.
            // Fresh databases already have this shape after OracleInitialCreate.
            migrationBuilder.Sql("""
                DECLARE
                    n NUMBER;
                BEGIN
                    SELECT COUNT(*) INTO n FROM user_tables
                    WHERE table_name = 'VENDOR';
                    IF n <> 1 THEN
                        RAISE_APPLICATION_ERROR(-20070,
                            'Run database/02-oracle-registry-sync.sql before EF migrations.');
                    END IF;

                    SELECT COUNT(*) INTO n FROM user_tab_columns
                    WHERE table_name = 'API_ASSET'
                      AND column_name = 'PublishingApplicationId';
                    IF n <> 1 THEN
                        RAISE_APPLICATION_ERROR(-20071,
                            'Run database/02-oracle-registry-sync.sql before EF migrations.');
                    END IF;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "OracleRegistrySync preserves legacy data and cannot be rolled back automatically.");
        }
    }
}
