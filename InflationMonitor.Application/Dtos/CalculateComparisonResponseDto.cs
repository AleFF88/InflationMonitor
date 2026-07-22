namespace InflationMonitor.Application.Dtos {
    public class CalculateComparisonResponseDto {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal InitialAmount { get; set; }
        public CashUsdComparisonSummaryDto Summary { get; set; } = new();
    }
}
