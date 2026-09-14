using System.Collections.ObjectModel;

namespace InflationMonitor.Application.Common.Constants {
    public static class FinancialInstrumentCategories {
        public const string Inflation = "Inflation";
        public const string Currencies = "Currencies";

        // Minimal supported boundary for national inflation data recording
        public static readonly DateOnly InflationMinSupportedDate = new(2000, 1, 1);
    }

    public static class CurrencyCodes {
        public const string Usd = "USD";
        // Minimal supported boundary for UAH/USD (Monetary reform, Sept 1996)
        public static readonly DateOnly UsdMinSupportedDate = new(1996, 9, 1);

        public const string Eur = "EUR";
        // Minimal supported boundary for UAH/EUR (Non-cash Euro introduced in Jan 1999)
        public static readonly DateOnly EurMinSupportedDate = new(1999, 1, 1);

        public static readonly ReadOnlyDictionary<string, DateOnly> CurrencyMinSupportedDates =
            new ReadOnlyDictionary<string, DateOnly>(new Dictionary<string, DateOnly>(StringComparer.OrdinalIgnoreCase) {
            { Usd, UsdMinSupportedDate },
            { Eur, EurMinSupportedDate }
        });

    }
}
