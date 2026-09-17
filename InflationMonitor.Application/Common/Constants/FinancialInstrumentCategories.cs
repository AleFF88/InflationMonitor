using System.Collections.Frozen;

namespace InflationMonitor.Application.Common.Constants {
    /// <summary>
    /// Provides shared category keys for classifying different types of financial instruments.
    /// </summary>
    public static class FinancialInstrumentCategories {
        /// <summary>
        /// Category key for inflation-related calculations.
        /// </summary>
        public const string Inflation = "Inflation";

        /// <summary>
        /// Category key for currency exchange rate calculations.
        /// </summary>
        public const string Currencies = "Currencies";
    }

    /// <summary>
    /// Provides constants and historical boundary configurations for inflation calculations.
    /// </summary>
    public static class InflationConstants {
        /// <summary>
        /// Gets the category key associated with inflation strategies.
        /// </summary>
        public const string CategoryKey = FinancialInstrumentCategories.Inflation;

        /// <summary>
        /// Defines standard instrument codes used in inflation monitoring.
        /// </summary>
        public static class Codes {
            /// <summary>
            /// Consumer Price Index instrument code.
            /// </summary>
            public const string Cpi = "CPI"; 
        }

        /// <summary>
        /// Gets the frozen dictionary containing the minimum supported historical dates for each inflation instrument.
        /// </summary>
        public static readonly FrozenDictionary<string, DateOnly> InflationMinSupportedDates =
            new Dictionary<string, DateOnly> { 
            // Minimal supported boundary for national inflation data recording
            { Codes.Cpi, new DateOnly(2000, 1, 1) }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Provides constants and historical boundary configurations for currency exchange calculations.
    /// </summary>
    public static class CurrencyConstants {
        /// <summary>
        /// Gets the category key associated with currency strategies.
        /// </summary>
        public const string CategoryKey = FinancialInstrumentCategories.Currencies;

        /// <summary>
        /// Defines standard currency codes supported by the system.
        /// </summary>
        public static class Codes {
            /// <summary>
            /// United States Dollar currency code.
            /// </summary>
            public const string Usd = "USD";

            /// <summary>
            /// Euro currency code.
            /// </summary>
            public const string Eur = "EUR";
        }

        /// <summary>
        /// Gets the frozen dictionary containing the minimum supported historical dates for each currency rate.
        /// </summary>
        public static readonly FrozenDictionary<string, DateOnly> CurrencyMinSupportedDates =
            new Dictionary<string, DateOnly> { 
            // Minimal supported boundary for UAH/USD (Monetary reform, Sept 1996)
            { Codes.Usd, new DateOnly(1996, 9, 1) },
            // Minimal supported boundary for UAH/EUR (Non-cash Euro introduced in Jan 1999)
            { Codes.Eur, new DateOnly(1999, 1, 1) }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}