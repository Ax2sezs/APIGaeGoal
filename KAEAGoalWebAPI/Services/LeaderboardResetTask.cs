using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KAEAGoalWebAPI.Services
{
    public class LeaderboardResetTask : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LeaderboardResetTask> _logger;
        public LeaderboardResetTask(IServiceProvider serviceProvider, ILogger<LeaderboardResetTask> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private DateTime GetBangkokTime()
        {
            var bangkokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkokTimeZone);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = GetBangkokTime();
                var firstOfNextYear = new DateTime(now.Year + 1, 1, 1); // Next Jan 1st
                var delay = firstOfNextYear - now;

                if (delay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation($"[LeaderboardResetTask] Waiting for {delay.TotalDays} days until the next reset.");

                    // 🚀 FIX: Break long delays into smaller 1-day (86400000ms) chunks
                    while (delay.TotalMilliseconds > int.MaxValue)
                    {
                        await Task.Delay(int.MaxValue, stoppingToken);
                        delay = firstOfNextYear - GetBangkokTime(); // Recalculate remaining time
                    }

                    await Task.Delay(delay, stoppingToken); // Final delay before execution
                }
                else
                {
                    _logger.LogWarning("[LeaderboardResetTask] Calculated delay is negative! Resetting immediately.");
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var leaderboardService = scope.ServiceProvider.GetRequiredService<LeaderboardService>();
                    await leaderboardService.ResetAndRewardLeaderboardAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LeaderboardResetTask] Error occurred while resetting leaderboard.");
                }
            }
        }
    }
}
