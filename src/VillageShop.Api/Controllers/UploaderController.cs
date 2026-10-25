using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VillageShop.Infrastructure.Uploader;

namespace VillageShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Allow both anonymous (with header fallback) and authenticated
public class UploaderController : ControllerBase
{
    private readonly IExcelUploaderService _uploaderService;

    public UploaderController(IExcelUploaderService uploaderService)
    {
        _uploaderService = uploaderService;
    }

    private long GetTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var tidHeader) && long.TryParse(tidHeader, out var tid) && tid > 0)
        {
            return tid;
        }
        if (long.TryParse(User.FindFirst("tenant_id")?.Value, out var claimTid) && claimTid > 0)
        {
            return claimTid;
        }
        return 1;
    }

    private long GetUserId()
    {
        if (long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) && uid > 0)
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
        var bytes = await _uploaderService.GenerateTemplateAsync(typeCode);
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
        var batch = await _uploaderService.GetBatchStatusAsync(batchId);
        if (batch == null) return NotFound(new { status = false, message = "Batch not found." });
        return Ok(batch);
    }

    [HttpGet("batch/{batchId}/rows")]
    public async Task<IActionResult> GetBatchRows(Guid batchId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var (total, rows) = await _uploaderService.GetBatchRowsAsync(batchId, status, page, pageSize);
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(new { total, page, pageSize, rows });
    }

    [HttpGet("batch/{batchId}/report")]
    public async Task<IActionResult> DownloadReport(Guid batchId)
    {
        var bytes = await _uploaderService.GenerateBatchReportAsync(batchId);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Batch_Report_{batchId}.xlsx");
    }

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var batches = await _uploaderService.GetBatchesPagedAsync(GetTenantId(), page, pageSize);
        return Ok(batches);
    }
}
