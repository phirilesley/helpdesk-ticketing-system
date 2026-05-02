using HelpDeskSystem.API.Setup;
using HelpDeskSystem.Application.DTOs.Integrations;
using HelpDeskSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/inbound/email")]
public class InboundEmailController : ControllerBase
{
    private readonly InboundEmailOptions _options;
    private readonly IEmailIngestionService _emailIngestionService;

    public InboundEmailController(InboundEmailOptions options, IEmailIngestionService emailIngestionService)
    {
        _options = options;
        _emailIngestionService = emailIngestionService;
    }

    [HttpPost]
    public async Task<ActionResult<InboundEmailResultDto>> Receive([FromBody] InboundEmailRequestDto request)
    {
        if (!_options.Enabled)
            return NotFound();

        var incomingSecret = Request.Headers["X-Inbound-Email-Secret"].ToString();
        if (string.IsNullOrWhiteSpace(_options.SharedSecret) || !string.Equals(incomingSecret, _options.SharedSecret, StringComparison.Ordinal))
            return Unauthorized();

        var result = await _emailIngestionService.ProcessInboundAsync(request, HttpContext.RequestAborted);
        if (!result.Success && result.Status != "DUPLICATE")
            return BadRequest(result);

        return Ok(result);
    }
}
