using ApexBuild.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedDataController : ControllerBase
    {
        private readonly DatabaseSeeder _seeder;
        private readonly ILogger<SeedDataController> _logger;

        public SeedDataController(DatabaseSeeder seeder, ILogger<SeedDataController> logger)
        {
            _seeder = seeder;
            _logger = logger;
        }

        /// <summary>
        /// Seeds the database with comprehensive test data
        /// </summary>
        /// <returns>Result of seeding operation</returns>
        [HttpPost("seed")]
        [AllowAnonymous] // Remove this in production - should be restricted to SuperAdmin
        public async Task<IActionResult> SeedDatabase(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Seed database endpoint called");
                
                var (success, message) = await _seeder.SeedAsync(cancellationToken);
                
                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = message,
                        timestamp = DateTime.UtcNow
                    });
                }
                
                return BadRequest(new
                {
                    success = false,
                    message = message,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in seed database endpoint");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Clears all seeded test data from the database
        /// </summary>
        /// <returns>Result of clear operation</returns>
        [HttpDelete("clear")]
        [AllowAnonymous] // Remove this in production - should be restricted to SuperAdmin
        public async Task<IActionResult> ClearSeededData(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Clear seeded data endpoint called");
                
                var (success, message) = await _seeder.ClearSeededDataAsync(cancellationToken);
                
                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = message,
                        timestamp = DateTime.UtcNow
                    });
                }
                
                return BadRequest(new
                {
                    success = false,
                    message = message,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in clear seeded data endpoint");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Checks if the database has been seeded
        /// </summary>
        /// <returns>Seeding status</returns>
        [HttpGet("status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSeedStatus()
        {
            try
            {
                // Simple check - look for test user
                // This is a simple implementation, you would inject IUnitOfWork for a proper check
                return Ok(new
                {
                    message = "Use this endpoint to check if database has been seeded. Implementation requires IUnitOfWork injection.",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking seed status");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
