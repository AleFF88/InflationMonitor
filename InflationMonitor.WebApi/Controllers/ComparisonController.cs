using InflationMonitor.Application.Dtos;
using InflationMonitor.Application.Queries.CalculateComparison;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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
        /// <remarks>
        /// Calculations have monthly granularity: only the year and month of <c>startDate</c> and 
        /// <c>endDate</c> are used, the day is ignored. Dates are normalized to the 1st day of the 
        /// month, and the response returns the normalized dates (e.g. a request with <c>2023-01-15</c> 
        /// returns <c>2023-01-01</c>).
        /// </remarks>
        /// <param name="startDate">Start of the period in YYYY-MM-DD format. Only year and month are used; the day is ignored.</param>
        /// <param name="endDate">End of the period in YYYY-MM-DD format. Only year and month are used; the day is ignored.</param>
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
            [FromQuery, Required] DateOnly? startDate,
            [FromQuery, Required] DateOnly? endDate,
            [FromQuery, Required] decimal? amount,
            CancellationToken cancellationToken) {

            // Encapsulates incoming query string parameters into a MediatR query object
            var query = new CalculateComparisonQuery(startDate!.Value, endDate!.Value, amount!.Value);
            // Sends the query through the MediatR pipeline to be processed by CalculateComparisonQueryHandler
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
    }
}