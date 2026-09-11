using InflationMonitor.Application.Dtos;
using InflationMonitor.Application.Queries.CalculateComparison;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InflationMonitor.WebApi.Controllers {
    [ApiController]
    [Route("api/calculator")]
    [Produces("application/json")]
    public class ComparisonController : ControllerBase {
        private readonly IMediator _mediator;

        public ComparisonController(IMediator mediator) {
            _mediator = mediator;
        }

        /// <summary>
        /// Calculates changes in the purchasing power of the Ukrainian Hryvnia relative to various financial equivalents based on historical data.
        /// </summary>
        /// <param name="startDate">Start period of the calculation in YYYY-MM-DD format.</param>
        /// <param name="endDate">End period of the calculation in YYYY-MM-DD format.</param>
        /// <param name="amount">Initial monetary amount in UAH. Must be greater than 0.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Calculated financial comparison summary containing equivalents for requested instruments.</returns>
        /// <response code="200">Calculations successfully evaluated.</response>
        /// <response code="400">Invalid input parameters or business rule violation.</response>
        /// <response code="500">Internal server error occurred while processing calculation.</response>
        [HttpGet("compare")]
        [ProducesResponseType(typeof(CalculateComparisonResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Compare(
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] decimal amount,
            CancellationToken cancellationToken) {

            // Encapsulates incoming query string parameters into a MediatR query object
            var query = new CalculateComparisonQuery(startDate, endDate, amount);
            // Sends the query through the MediatR pipeline to be processed by CalculateComparisonQueryHandler
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
    }
}