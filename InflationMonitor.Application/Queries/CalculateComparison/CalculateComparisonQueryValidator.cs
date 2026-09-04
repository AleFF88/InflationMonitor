using FluentValidation;

namespace InflationMonitor.Application.Queries.CalculateComparison {
    public class CalculateComparisonQueryValidator : AbstractValidator<CalculateComparisonQuery> {

        // Historical fact: The Ukrainian hryvnia was officially introduced on September 2, 1996.
        // Technical mapping: Since domain entities process data on a monthly basis, we set
        //   the lower bound to the first day of that month (September 1, 1996) to cover
        //   the entire starting period.
        private static readonly DateTime MinSupportedDate = new(1996, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        public CalculateComparisonQueryValidator() {
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("StartDate cannot be later than EndDate.");

            // Normalize input date to the first day of the month (monthly basis)
            RuleFor(x => new DateTime(x.StartDate.Year, x.StartDate.Month, 1, 0, 0, 0, DateTimeKind.Utc ))
                .GreaterThanOrEqualTo(MinSupportedDate)
                .WithMessage("Historical data for the hryvnia is available only since September 2, 1996.");
        }
    }
}
