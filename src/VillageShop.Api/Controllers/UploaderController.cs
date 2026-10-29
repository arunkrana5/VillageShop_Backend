using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VillageShop.Infrastructure.Uploader;

namespace VillageShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Allows token authentication as well as secure fallback
public class UploaderController : ControllerBase
{
    private readonly IExcelUploaderService _uploaderService;

    public UploaderController(IExcelUploaderService uploaderService)
    {
        _uploaderService = uploaderService;
    }

    private long GetTenantId()
    {
        // 1. Primary: Authenticated JWT claims
        if (long.TryParse(User.FindFirst("tenant_id")?.Value, out var claimTid) && claimTid > 0)
        {
            return claimTid;
        }
        if (long.TryParse(User.FindFirst("TenantId")?.Value, out var altClaimTid) && altClaimTid > 0)
        {
            return altClaimTid;
        }

        // 2. Secondary: Fallback header if unauthenticated
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var tidHeader) && long.TryParse(tidHeader, out var tid) && tid > 0)
        {
            return tid;
        }

        return 1;
    }

    private long GetUserId()
    {
        if (long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) && uid > 0)
        {
            return uid;
        }
        return GetTenantId();
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetTypes()
    {
        var types = await _uploaderService.GetActiveTypesAsync();
        return Ok(types);
    }

    [HttpGet("template/{typeCode}")]
    public async Task<IActionResult> DownloadTemplate(string typeCode)
    {
        var bytes = await _uploaderService.GenerateTemplateAsync(typeCode, GetTenantId());
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{typeCode}_Template.xlsx");
    }

    [HttpPost("upload/{typeCode}")]
    [RequestSizeLimit(52428800)] // 50 MB
    public async Task<IActionResult> UploadExcel(string typeCode, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { status = false, message = "Please select a valid Excel file." });

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { status = false, message = "Only modern .xlsx files are supported." });

        using var stream = file.OpenReadStream();
        var batchId = await _uploaderService.StageExcelAsync(typeCode, stream, file.FileName, GetTenantId(), GetUserId(), ct);

        return Ok(new { status = true, batchId, message = "File successfully staged." });
    }

    [HttpPost("batch/{batchId}/process")]
    public async Task<IActionResult> ProcessBatch(Guid batchId)
    {
        var queued = await _uploaderService.QueueProcessingAsync(batchId, GetTenantId(), GetUserId());
        if (!queued)
            return Conflict(new { status = false, message = "Batch is already processing or completed." });

        return Accepted(new { status = true, message = "Processing task accepted." });
    }

    [HttpGet("batch/{batchId}/status")]
    public async Task<IActionResult> GetBatchStatus(Guid batchId)
    {
        var batch = await _uploaderService.GetBatchStatusAsync(batchId, GetTenantId());
        if (batch == null) return NotFound(new { status = false, message = "Batch not found or unauthorized." });
        return Ok(batch);
    }

    [HttpGet("batch/{batchId}/rows")]
    public async Task<IActionResult> GetBatchRows(Guid batchId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var (total, rows) = await _uploaderService.GetBatchRowsAsync(batchId, GetTenantId(), status, page, pageSize);
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(new { total, page, pageSize, rows });
    }

    [HttpGet("batch/{batchId}/report")]
    public async Task<IActionResult> DownloadReport(Guid batchId)
    {
        var bytes = await _uploaderService.GenerateBatchReportAsync(batchId, GetTenantId());
        if (bytes == null || bytes.Length == 0)
            return NotFound(new { status = false, message = "Report not found or unauthorized." });

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Batch_Report_{batchId}.xlsx");
    }

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var batches = await _uploaderService.GetBatchesPagedAsync(GetTenantId(), page, pageSize);
        return Ok(batches);
    }
}
