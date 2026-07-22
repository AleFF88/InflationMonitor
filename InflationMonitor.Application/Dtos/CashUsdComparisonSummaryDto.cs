namespace InflationMonitor.Application.Dtos {
    public class CashUsdComparisonSummaryDto {
        public decimal CashGrivna { get; set; }
        public decimal InflationEquivalent { get; set; }
        public decimal UsdEquivalent { get; set; }
    }
}
