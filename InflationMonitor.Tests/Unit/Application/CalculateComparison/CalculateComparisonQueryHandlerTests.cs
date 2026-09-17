using FluentAssertions;
using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Application.Queries.CalculateComparison;
using Moq;

namespace InflationMonitor.Tests.Unit.Application.CalculateComparison {
    /// <summary>
    /// Unit tests for the <see cref="CalculateComparisonQueryHandler"/> class.
    /// Validates strategy factory orchestration, key resolution, data mapping to DTO, 
    /// and warning collection from multiple strategies.
    /// </summary>
    public class CalculateComparisonQueryHandlerTests {
        private readonly Mock<IFinancialInstrumentFactory> _factoryMock;
        private readonly Mock<IBatchFinancialInstrumentStrategy> _inflationStrategyMock;
        private readonly Mock<IBatchFinancialInstrumentStrategy> _currencyStrategyMock;
        private readonly CalculateComparisonQueryHandler _handler;

        public CalculateComparisonQueryHandlerTests() {
            _factoryMock = new Mock<IFinancialInstrumentFactory>();
            _inflationStrategyMock = new Mock<IBatchFinancialInstrumentStrategy>();
            _currencyStrategyMock = new Mock<IBatchFinancialInstrumentStrategy>();

            _factoryMock
                .Setup(f => f.GetStrategy(FinancialInstrumentCategories.Inflation))
                .Returns(_inflationStrategyMock.Object);

            _factoryMock
                .Setup(f => f.GetStrategy(FinancialInstrumentCategories.Currencies))
                .Returns(_currencyStrategyMock.Object);

            _handler = new CalculateComparisonQueryHandler(_factoryMock.Object);
        }

        /// <summary>
        /// Verifies that the handler requests strategies from the factory using the correct 
        /// category keys and passes the expected parameters to each strategy's calculation method.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldOrchestrateCallsAndRequestStrategiesWithCorrectKeysAndParameters() {
            // Arrange
            var query = new CalculateComparisonQuery(new DateOnly(2023, 1, 1), new DateOnly(2023, 5, 1), 1000m);
            var cancellationToken = new CancellationTokenSource().Token;

            var emptyInflationResult = new CalculationResult(new Dictionary<string, decimal?>(), []);
            var emptyCurrencyResult = new CalculationResult(new Dictionary<string, decimal?>(), []);

            _inflationStrategyMock
                .Setup(s => s.CalculateEquivalentsAsync(
                    It.IsAny<IEnumerable<string>>(), query.StartDate, query.EndDate, query.Amount, cancellationToken))
                .ReturnsAsync(emptyInflationResult);

            _currencyStrategyMock
                .Setup(s => s.CalculateEquivalentsAsync(
                    It.IsAny<IEnumerable<string>>(), query.StartDate, query.EndDate, query.Amount, cancellationToken))
                .ReturnsAsync(emptyCurrencyResult);

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _factoryMock.Verify(f => f.GetStrategy(FinancialInstrumentCategories.Inflation), Times.Once);
            _factoryMock.Verify(f => f.GetStrategy(FinancialInstrumentCategories.Currencies), Times.Once);

            _inflationStrategyMock.Verify(s => s.CalculateEquivalentsAsync(
                It.Is<IEnumerable<string>>(codes => !codes.Any()),
                query.StartDate,
                query.EndDate,
                query.Amount,
                cancellationToken), Times.Once);

            _currencyStrategyMock.Verify(s => s.CalculateEquivalentsAsync(
                It.Is<IEnumerable<string>>(codes => codes.SequenceEqual(new[] { CurrencyConstants.Codes.Usd, CurrencyConstants.Codes.Eur })),
                query.StartDate,
                query.EndDate,
                query.Amount,
                cancellationToken), Times.Once);
        }

        /// <summary>
        /// Verifies that the handler correctly maps calculation results from strategies to <see cref="CalculateComparisonResponseDto"/>,
        /// including combining warnings from both strategies.
        /// </summary>
        [Fact]
        public async Task Handle_WhenStrategiesReturnData_ShouldMapResponseDtoAndCombineWarningsCorrectly() {
            // Arrange
            var query = new CalculateComparisonQuery(new DateOnly(2023, 1, 1), new DateOnly(2023, 5, 1), 1000m);

            var inflationEquivalents = new Dictionary<string, decimal?> {
                { InflationConstants.Codes.Cpi, 1050.50m }
            };
            var inflationWarnings = new List<string> { "Inflation warning 1" };
            var inflationResult = new CalculationResult(inflationEquivalents, inflationWarnings);

            var currencyEquivalents = new Dictionary<string, decimal?> {
                { CurrencyConstants.Codes.Usd, 1020.00m },
                { CurrencyConstants.Codes.Eur, 1010.25m }
            };
            var currencyWarnings = new List<string> { "Currency warning 1" };
            var currencyResult = new CalculationResult(currencyEquivalents, currencyWarnings);

            _inflationStrategyMock
                .Setup(s => s.CalculateEquivalentsAsync(It.IsAny<IEnumerable<string>>(), query.StartDate, query.EndDate, query.Amount, It.IsAny<CancellationToken>()))
                .ReturnsAsync(inflationResult);

            _currencyStrategyMock
                .Setup(s => s.CalculateEquivalentsAsync(It.IsAny<IEnumerable<string>>(), query.StartDate, query.EndDate, query.Amount, It.IsAny<CancellationToken>()))
                .ReturnsAsync(currencyResult);

            // Act
            var response = await _handler.Handle(query, CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.StartDate.Should().Be("2023-01-01");
            response.EndDate.Should().Be("2023-05-01");
            response.InitialAmount.Should().Be(1000m);

            response.Summary.CashGrivna.Should().Be(1000m);
            response.Summary.InflationEquivalent.Should().Be(1050.50m);
            response.Summary.UsdEquivalent.Should().Be(1020.00m);
            response.Summary.EurEquivalent.Should().Be(1010.25m);

            response.Warnings.Should().HaveCount(2);
            response.Warnings.Should().ContainInOrder("Inflation warning 1", "Currency warning 1");
        }

        /// <summary>
        /// Verifies that when strategies return null or missing dictionary entries for equivalents,
        /// the handler assigns null values to summary properties without throwing exceptions.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEquivalentsAreMissingInResults_ShouldAssignNullToSummaryProperties() {
            // Arrange
            var query = new CalculateComparisonQuery(new DateOnly(2023, 1, 1), new DateOnly(2023, 5, 1), 1000m);

            var inflationResult = new CalculationResult(new Dictionary<string, decimal?>(), []);
            var currencyResult = new CalculationResult(new Dictionary<string, decimal?> {
                { CurrencyConstants.Codes.Usd, 1020.00m }
                // EUR key is missing completely
            }, []);

            _inflationStrategyMock
                .Setup(s => s.CalculateEquivalentsAsync(It.IsAny<IEnumerable<string>>(), query.StartDate, query.EndDate, query.Amount, It.IsAny<CancellationToken>()))
                .ReturnsAsync(inflationResult);

            _currencyStrategyMock
                .Setup(s => s.CalculateEquivalentsAsync(It.IsAny<IEnumerable<string>>(), query.StartDate, query.EndDate, query.Amount, It.IsAny<CancellationToken>()))
                .ReturnsAsync(currencyResult);

            // Act
            var response = await _handler.Handle(query, CancellationToken.None);

            // Assert
            response.Summary.InflationEquivalent.Should().BeNull();
            response.Summary.UsdEquivalent.Should().Be(1020.00m);
            response.Summary.EurEquivalent.Should().BeNull();
        }
    }
}