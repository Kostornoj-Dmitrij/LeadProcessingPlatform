using Microsoft.AspNetCore.Mvc;

namespace LeadService.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateLead([FromBody] CreateLeadRequest request)
    {
        return Accepted(new { 
            leadId = Guid.NewGuid(),
            message = "Lead received and will be processed asynchronously"
        });
    }
    
    [HttpGet]
    public IActionResult GetLeads()
    {
        return Ok(new[] { new { id = Guid.NewGuid(), email = "test@example.com" } });
    }
}

public record CreateLeadRequest(
    string Email, 
    string CompanyName, 
    string Source,
    string? ContactPerson,
    string? Phone
);