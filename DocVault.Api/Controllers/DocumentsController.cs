using System.Security.Claims;
using DocVault.Application.DTOs.Common;
using DocVault.Application.DTOs.Documents;
using DocVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocVault.Api.Controllers;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _docs;
    public DocumentsController(IDocumentService docs) => _docs = docs;

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id)
            ? id
            : throw new UnauthorizedAccessException("Session expired. Please log in again.");

    [HttpPost]
    [RequestSizeLimit(26_214_400)]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> Upload(
        [FromForm] UploadDocumentRequest request,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<DocumentDto>.Fail("No file provided.", HttpContext.TraceIdentifier));

        using var stream = file.OpenReadStream();
        var result = await _docs.UploadAsync(request, stream, file.FileName, file.ContentType, CurrentUserId, ct);
        return Ok(ApiResponse<DocumentDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<DocumentDto>>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? financialYearId = null,
        [FromQuery] int? documentTypeId = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _docs.GetUserDocumentsAsync(CurrentUserId, page, pageSize, financialYearId, documentTypeId, searchTerm, ct);
        return Ok(ApiResponse<PagedResult<DocumentDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _docs.GetByIdAsync(id, CurrentUserId, isAdmin: false, ct);
        if (result == null) return NotFound(ApiResponse<DocumentDto>.Fail("Document not found.", HttpContext.TraceIdentifier));
        return Ok(ApiResponse<DocumentDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var doc = await _docs.GetByIdAsync(id, CurrentUserId, isAdmin: false, ct);
        if (doc == null) return NotFound();
        var stream = await _docs.DownloadAsync(id, CurrentUserId, isAdmin: false, ct);
        return File(stream, doc.ContentType, doc.OriginalFileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _docs.SoftDeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Document deleted. Can be restored within 30 days." }, HttpContext.TraceIdentifier));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> Update(Guid id, [FromBody] UpdateDocumentRequest request, CancellationToken ct)
    {
        var result = await _docs.UpdateDocumentAsync(id, CurrentUserId, isAdmin: false, request, ct);
        return Ok(ApiResponse<DocumentDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<ApiResponse<object>>> Restore(Guid id, CancellationToken ct)
    {
        await _docs.RestoreAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Document restored." }, HttpContext.TraceIdentifier));
    }
}
