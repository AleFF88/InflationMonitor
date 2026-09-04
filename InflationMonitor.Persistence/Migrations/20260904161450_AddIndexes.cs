using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InflationMonitor.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InflationRates_Year_Month",
                table: "InflationRates",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CurrencyCode_Year_Month",
                table: "ExchangeRates",
                columns: new[] { "CurrencyCode", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InflationRates_Year_Month",
                table: "InflationRates");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_CurrencyCode_Year_Month",
                table: "ExchangeRates");
        }
    }
}
