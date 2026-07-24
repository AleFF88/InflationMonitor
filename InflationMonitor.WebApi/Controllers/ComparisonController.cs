using InflationMonitor.Application.Queries.CalculateComparison;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InflationMonitor.WebApi.Controllers {
    [ApiController]
    [Route("api/calculator")]
    public class ComparisonController : ControllerBase {
        private readonly IMediator _mediator;

        public ComparisonController(IMediator mediator) {
            _mediator = mediator;
        }

        [HttpGet("compare")]
        public async Task<IActionResult> Compare(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
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