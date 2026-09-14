using System.Collections.Frozen;

namespace InflationMonitor.Application.Common.Constants {
    public static class FinancialInstrumentCategories {
        public const string Inflation = "Inflation";
        public const string Currencies = "Currencies";
    }

    public static class InflationConstants {
        public const string CategoryKey = FinancialInstrumentCategories.Inflation;

        public static class Codes {
            public const string Cpi = "CPI"; // Consumer Price Index
        }

        public static readonly FrozenDictionary<string, DateOnly> InflationMinSupportedDates = 
            new Dictionary<string, DateOnly> { 
            // Minimal supported boundary for national inflation data recording
            { Codes.Cpi, new DateOnly(2000, 1, 1) }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public static class CurrencyConstants {
        public const string CategoryKey = FinancialInstrumentCategories.Currencies;

        public static class Codes {
            public const string Usd = "USD";
            public const string Eur = "EUR";
        }


        public static readonly FrozenDictionary<string, DateOnly> CurrencyMinSupportedDates =
            new Dictionary<string, DateOnly> { 
            // Minimal supported boundary for UAH/USD (Monetary reform, Sept 1996)
            { Codes.Usd, new DateOnly(1996, 9, 1) },
            // Minimal supported boundary for UAH/EUR (Non-cash Euro introduced in Jan 1999)
            { Codes.Eur, new DateOnly(1999, 1, 1) }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    }
}
