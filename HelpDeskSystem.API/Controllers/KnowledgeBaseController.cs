using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.DTOs.KnowledgeBase;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Route("api/knowledge-base")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly HelpDeskDbContext _context;

    public KnowledgeBaseController(IKnowledgeBaseService knowledgeBaseService, HelpDeskDbContext context)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _context = context;
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<KnowledgeBaseCategoryDto>>> GetCategories([FromQuery] string? tenantDomain = null)
    {
        var tenantId = await ResolveTenantIdAsync(tenantDomain);
        if (!tenantId.HasValue)
            return BadRequest("Tenant resolution failed.");

        var includePrivate = User.IsInRole("Agent") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        var categories = await _knowledgeBaseService.GetCategoriesAsync(tenantId.Value, includePrivate, HttpContext.RequestAborted);
        return Ok(categories);
    }

    [HttpGet("articles")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<KnowledgeBaseArticleDto>>> SearchArticles(
        [FromQuery] string? tenantDomain = null,
        [FromQuery] string? q = null,
        [FromQuery] int? categoryId = null)
    {
        var tenantId = await ResolveTenantIdAsync(tenantDomain);
        if (!tenantId.HasValue)
            return BadRequest("Tenant resolution failed.");

        var articles = await _knowledgeBaseService.SearchPublishedArticlesAsync(
            tenantId.Value,
            q,
            categoryId,
            HttpContext.RequestAborted);
        return Ok(articles);
    }

    [HttpGet("articles/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<KnowledgeBaseArticleDto>> GetArticleBySlug(string slug, [FromQuery] string? tenantDomain = null)
    {
        var tenantId = await ResolveTenantIdAsync(tenantDomain);
        if (!tenantId.HasValue)
            return BadRequest("Tenant resolution failed.");

        var article = await _knowledgeBaseService.GetPublishedArticleBySlugAsync(tenantId.Value, slug, HttpContext.RequestAborted);
        if (article == null)
            return NotFound();

        return Ok(article);
    }

    [HttpPost("articles/{articleId}/feedback")]
    [Authorize]
    public async Task<IActionResult> AddFeedback(int articleId, [FromBody] KnowledgeBaseFeedbackRequestDto dto)
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var userId = User.GetUserId();
        await _knowledgeBaseService.AddFeedbackAsync(tenantId.Value, articleId, userId, dto, HttpContext.RequestAborted);
        return NoContent();
    }

    private async Task<int?> ResolveTenantIdAsync(string? tenantDomain)
    {
        var tenantIdClaim = User.GetTenantId();
        if (tenantIdClaim.HasValue)
            return tenantIdClaim.Value;

        if (string.IsNullOrWhiteSpace(tenantDomain))
            return null;

        return await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Domain == tenantDomain && t.IsActive)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(HttpContext.RequestAborted);
    }
}
