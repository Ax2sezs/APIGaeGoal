using System;
using System.Security.Claims;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace KAEAGoalWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;
        private readonly IServiceProvider _serviceProvider;

        public LeaderboardController(ILeaderboardService leaderboardService, IServiceProvider serviceProvider)
        {
            _leaderboardService = leaderboardService;
            _serviceProvider = serviceProvider;
        }

        [HttpGet("Get-Current-Leaderboard")]
        public async Task<IActionResult> GetCurrentLeaderboard()
        {
            try
            {
                var leaderboard = await _leaderboardService.GetCurrentLeaderboardAsync();
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("test-reset")]
        public async Task<IActionResult> TestResetLeaderboard()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var leaderboardService = scope.ServiceProvider.GetRequiredService<ILeaderboardService>(); // ✅ Use Interface

                await leaderboardService.ResetAndRewardLeaderboardAsync();
                return Ok("Leaderboard reset successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpGet("top10")]
        public async Task<IActionResult> GetTop10Leaderboard()
        {
            var top10 = await _leaderboardService.GetTop10LeaderboardAsync();
            return Ok(top10);
        }

        //[Authorize]
        [HttpGet("ranking/me")]
        public async Task<IActionResult> GetYourCurrentRanking()
        {
            // ดึง userId จาก JWT token โดยไม่ต้องกรอกเอง
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            //var currentUserId = Guid.Parse("A542677B-B038-4407-80AE-A60F1EC5CD33");

            // เรียกใช้ service เพื่อดึงข้อมูลอันดับของผู้ใช้
            var ranking = await _leaderboardService.GetYourCurrentRankingAsync(currentUserId);

            if (ranking == null)
                return NotFound(new { message = "User not found in the leaderboard." });

            return Ok(ranking); // ส่งข้อมูลอันดับของผู้ใช้กลับมา
        }



    }
}
