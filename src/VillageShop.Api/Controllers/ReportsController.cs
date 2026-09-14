using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillageShop.Common.Models;

namespace VillageShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    [HttpGet("dashboard-summary")]
    public IActionResult GetDashboardSummary()
    {
        return Ok(new PostResponse
        {
            Status = true,
            StatusCode = 200,
            Message = "Dashboard summary fetched successfully.",
            ID = 1
        });
    }
}
