using MediatR;
using Microsoft.AspNetCore.Mvc;
using LeadService.Application.Commands.CreateLead;
using LeadService.Application.Queries.GetLeadById;
using LeadService.Application.Queries.GetLeadsByStatus;
using LeadService.Application.DTOs;
using LeadService.Domain.Enums;

namespace LeadService.Host.Controllers;

/// <summary>
/// Контроллер для управления лидами
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LeadsController(IMediator mediator, ILogger<LeadsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(LeadDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LeadDto>> CreateLead(
        [FromBody] CreateLeadCommand command,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        logger.LogInformation("Received request to create lead from source: {Source}", command.Source);

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            command.ExternalLeadId = idempotencyKey;
        }

        var result = await mediator.Send(command);
        
        return Accepted(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeadDto>> GetLeadById(Guid id)
    {
        var query = new GetLeadByIdQuery { Id = id };
        var result = await mediator.Send(query);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<LeadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LeadDto>>> GetLeadsByStatus(
        [FromQuery] LeadStatus? status = null,
        [FromQuery] int? limit = null,
        [FromQuery] int? offset = null)
    {
        var query = new GetLeadsByStatusQuery
        {
            Status = status,
            Limit = limit,
            Offset = offset
        };

        var result = await mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}