using Microsoft.AspNetCore.Mvc;
using Server_DotNetCore.API.Services;

namespace Server_DotNetCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly RedisService _redisService;

    public StatsController(RedisService redisService)
    {
        _redisService = redisService;
    }

    [HttpGet("active-clients")]
    public async Task<IActionResult> GetActiveClients()
    {
        try
        {
            var count = await _redisService.GetActiveUsersAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            // its  much better to have a global error handler instead of try catch many times. i did not as its a small test
            return StatusCode(500, new { error = $"Error reading from Redis: {ex.Message}" });
        }
    }

    [HttpGet("file-count")]
    public async Task<IActionResult> GetFileCount()
    {
        try
        {
            var count = await _redisService.GetFileCountAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            // its  much better to have a global error handler instead of try catch many times. 
            return StatusCode(500, new { error = $"Error reading from Redis: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var activeClients = await _redisService.GetActiveUsersAsync();
            var fileCount = await _redisService.GetFileCountAsync();
            return Ok(new 
            { 
                activeClients,
                fileCount 
            });
        }
        catch (Exception ex)
        {
            // its  much better to have a global error handler instead of try catch many times. 
            return StatusCode(500, new { error = $"Error reading from Redis: {ex.Message}" });
        }
    }
}

