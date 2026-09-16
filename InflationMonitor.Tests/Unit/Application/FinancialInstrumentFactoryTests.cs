using FluentAssertions;
using InflationMonitor.Application.Common.Constants;
using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Application.Factories;
using Moq;

namespace InflationMonitor.Tests.Unit.Application {
    /// <summary>
    /// Unit tests for the <see cref="FinancialInstrumentFactory"/> implementation.
    /// Verifies strategy resolution by category key and handling of unsupported categories.
    /// </summary>
    public class FinancialInstrumentFactoryTests {
        /// <summary>
        /// Verifies that providing a supported category key correctly resolves the matching strategy instance regardless of string casing.
        /// </summary>
        [Fact]
        public void GetStrategy_WhenCategoryIsSupported_ShouldReturnMatchingStrategy() {
            // Arrange
            var mockInflationStrategy = new Mock<IBatchFinancialInstrumentStrategy>();
            mockInflationStrategy.SetupGet(s => s.CategoryKey).Returns(FinancialInstrumentCategories.Inflation);

            var mockCurrencyStrategy = new Mock<IBatchFinancialInstrumentStrategy>();
            mockCurrencyStrategy.SetupGet(s => s.CategoryKey).Returns(FinancialInstrumentCategories.Currencies);

            var factory = new FinancialInstrumentFactory([mockInflationStrategy.Object, mockCurrencyStrategy.Object]);

            // Act
            var result = factory.GetStrategy("inflation"); // Lowercase check

            // Assert
            result.Should().BeSameAs(mockInflationStrategy.Object);
        }

        /// <summary>
        /// Verifies that requesting an unknown or unsupported category key throws a <see cref="NotSupportedException"/>.
        /// </summary>
        [Fact]
        public void GetStrategy_WhenCategoryIsUnsupported_ShouldThrowNotSupportedException() {
            // Arrange
            var mockInflationStrategy = new Mock<IBatchFinancialInstrumentStrategy>();
            mockInflationStrategy.SetupGet(s => s.CategoryKey).Returns(FinancialInstrumentCategories.Inflation);

            var factory = new FinancialInstrumentFactory([mockInflationStrategy.Object]);

            // Act
            Action act = () => factory.GetStrategy("Dummy");

            // Assert
            act.Should().Throw<NotSupportedException>()
               .WithMessage("*category 'Dummy' is not supported*");
        }
    }
}