using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Data;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/demo-data")]
[Authorize(Roles = "SuperAdmin")]
public class DemoDataController : ControllerBase
{
    private readonly MasterDbContext _context;
    private readonly ILogger<DemoDataController> _logger;

    public DemoDataController(MasterDbContext context, ILogger<DemoDataController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedDemoData([FromQuery] bool clearExisting = true)
    {
        try
        {
            _logger.LogInformation("Demo data seeding endpoint called - currently disabled");
            return Ok(new
            {
                success = false,
                message = "Demo data seeding is currently disabled. The database is restored from backup and ready to use."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process demo data request");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetDemoDataStatus()
    {
        try
        {
            var demoTenantCount = await _context.Tenants.CountAsync(t => t.CompanyName.Contains("(DEMO)"));
            return Ok(new { success = true, hasData = demoTenantCount > 0, demoTenantCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check demo data status");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
