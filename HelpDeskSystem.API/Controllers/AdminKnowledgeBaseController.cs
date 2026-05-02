using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.DTOs.KnowledgeBase;
using HelpDeskSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/knowledge-base")]
public class AdminKnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public AdminKnowledgeBaseController(IKnowledgeBaseService knowledgeBaseService)
    {
        _knowledgeBaseService = knowledgeBaseService;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyCollection<KnowledgeBaseCategoryDto>>> GetCategories([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantIdForAdmin(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var categories = await _knowledgeBaseService.GetCategoriesAsync(resolvedTenantId.Value, includePrivate: true, HttpContext.RequestAborted);
        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<KnowledgeBaseCategoryDto>> CreateCategory([FromBody] UpsertKnowledgeBaseCategoryDto dto, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantIdForAdmin(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var result = await _knowledgeBaseService.UpsertCategoryAsync(resolvedTenantId.Value, null, dto, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPut("categories/{categoryId}")]
    public async Task<ActionResult<KnowledgeBaseCategoryDto>> UpdateCategory(int categoryId, [FromBody] UpsertKnowledgeBaseCategoryDto dto, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantIdForAdmin(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var result = await _knowledgeBaseService.UpsertCategoryAsync(resolvedTenantId.Value, categoryId, dto, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPost("articles")]
    public async Task<ActionResult<KnowledgeBaseArticleDto>> CreateArticle([FromBody] UpsertKnowledgeBaseArticleDto dto, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantIdForAdmin(tenantId);
        var actorUserId = User.GetUserId();
        if (!resolvedTenantId.HasValue || !actorUserId.HasValue)
            return Forbid();

        var result = await _knowledgeBaseService.UpsertArticleAsync(resolvedTenantId.Value, actorUserId.Value, null, dto, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPut("articles/{articleId}")]
    public async Task<ActionResult<KnowledgeBaseArticleDto>> UpdateArticle(int articleId, [FromBody] UpsertKnowledgeBaseArticleDto dto, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantIdForAdmin(tenantId);
        var actorUserId = User.GetUserId();
        if (!resolvedTenantId.HasValue || !actorUserId.HasValue)
            return Forbid();

        var result = await _knowledgeBaseService.UpsertArticleAsync(resolvedTenantId.Value, actorUserId.Value, articleId, dto, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpDelete("articles/{articleId}")]
    public async Task<IActionResult> DeleteArticle(int articleId, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantIdForAdmin(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var deleted = await _knowledgeBaseService.DeleteArticleAsync(resolvedTenantId.Value, articleId, HttpContext.RequestAborted);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    private int? ResolveTenantIdForAdmin(int? tenantIdFromQuery)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantIdFromQuery ?? User.GetTenantId();

        return User.GetTenantId();
    }
}
