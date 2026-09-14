using FluentValidation;
using InflationMonitor.Application.Common.Constants;

namespace InflationMonitor.Application.Queries.CalculateComparison {
    public class CalculateComparisonQueryValidator : AbstractValidator<CalculateComparisonQuery> {

        // Historical fact: The Ukrainian hryvnia was officially introduced on September 2, 1996.
        private static readonly DateOnly MinSupportedDate = CurrencyConstants.CurrencyMinSupportedDates[CurrencyConstants.Codes.Usd]; 

        public CalculateComparisonQueryValidator() {
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("StartDate cannot be later than EndDate.");

            // Normalize input date to the first day of the month (monthly basis)
            RuleFor(x => new DateOnly(x.StartDate.Year, x.StartDate.Month, 1))
                .GreaterThanOrEqualTo(MinSupportedDate)
                .WithMessage("Historical data for the hryvnia is available only since September 2, 1996.");
        }
    }
}
