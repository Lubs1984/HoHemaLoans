using Microsoft.AspNetCore.Mvc;
using HoHemaLoans.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ApplicationDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _logger.LogInformation("[HEALTH] Health check endpoint called");
        
        try
        {
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();
            
            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "HoHema Loans API",
                version = "1.0.0",
                database = new
                {
                    connected = canConnect,
                    connectionString = _context.Database.GetConnectionString()?.Substring(0, Math.Min(50, _context.Database.GetConnectionString()?.Length ?? 0)) + "..."
                }
            };

            _logger.LogInformation("[HEALTH] Health check passed - database connected: {connected}", canConnect);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HEALTH] Health check failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("db")]
    public async Task<IActionResult> DatabaseCheck()
    {
        _logger.LogInformation("[HEALTH-DB] Database check endpoint called");
        
        try
        {
            _logger.LogInformation("[HEALTH-DB] Testing database connection...");
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                _logger.LogWarning("[HEALTH-DB] Cannot connect to database");
                return StatusCode(503, new { status = "database_unavailable", error = "Cannot establish database connection" });
            }

            _logger.LogInformation("[HEALTH-DB] Testing database query...");
            // Test with a simple query
            var userCount = await _context.Users.CountAsync();
            
            _logger.LogInformation("[HEALTH-DB] Database check passed - User count: {count}", userCount);
            return Ok(new
            {
                status = "database_healthy",
                connected = true,
                userCount = userCount,
                connectionInfo = new
                {
                    provider = _context.Database.ProviderName,
                    connectionString = _context.Database.GetConnectionString()?.Substring(0, Math.Min(50, _context.Database.GetConnectionString()?.Length ?? 0)) + "..."
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HEALTH-DB] Database check failed: {message}", ex.Message);
            return StatusCode(503, new
            {
                status = "database_error",
                error = ex.Message,
                innerException = ex.InnerException?.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}