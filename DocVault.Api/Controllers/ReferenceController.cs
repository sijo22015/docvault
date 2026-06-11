using DocVault.Application.DTOs.Common;
using DocVault.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocVault.Api.Controllers;

[ApiController]
[Route("api/v1/reference")]
[Authorize]
public class ReferenceController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReferenceController(AppDbContext db) => _db = db;

    [HttpGet("departments")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetDepartments(CancellationToken ct)
    {
        var depts = await _db.Departments.Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name, d.Code }).ToListAsync(ct);
        return Ok(ApiResponse<IEnumerable<object>>.Ok(depts, HttpContext.TraceIdentifier));
    }

    [HttpGet("document-types")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetDocumentTypes(CancellationToken ct)
    {
        var types = await _db.DocumentTypes.Where(t => t.IsActive).OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name }).ToListAsync(ct);
        return Ok(ApiResponse<IEnumerable<object>>.Ok(types, HttpContext.TraceIdentifier));
    }

    [HttpGet("financial-years")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetFinancialYears(CancellationToken ct)
    {
        var fys = await _db.FinancialYears.OrderByDescending(f => f.FyStart)
            .Select(f => new { f.Id, f.Label, f.IsLocked, f.FyStart, f.FyEnd }).ToListAsync(ct);
        return Ok(ApiResponse<IEnumerable<object>>.Ok(fys, HttpContext.TraceIdentifier));
    }
}
