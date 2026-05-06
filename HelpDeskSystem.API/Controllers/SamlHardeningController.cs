using HelpDeskSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/saml-hardening")]
public class SamlHardeningController : ControllerBase
{
    private readonly ISamlFederationHardeningService _hardeningService;

    public SamlHardeningController(ISamlFederationHardeningService hardeningService)
    {
        _hardeningService = hardeningService;
    }

    [HttpPost("assess/{providerId:int}")]
    public async Task<ActionResult<SamlHardeningAssessment>> AssessProvider(
        int providerId,
        [FromBody] SamlAssessmentRequest request)
    {
        var result = await _hardeningService.AssessProviderAsync(providerId, request.SamlResponse, HttpContext.RequestAborted);
        if (!result.Passed)
            return StatusCode(422, result);
        return Ok(result);
    }
}

public class SamlAssessmentRequest
{
    public string? SamlResponse { get; set; }
}
