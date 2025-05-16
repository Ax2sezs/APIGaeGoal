using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Models;

namespace KAEAGoalWebAPI.Services
{
    public interface ILeaderboardService
    {
        Task ResetAndRewardLeaderboardAsync();
        Task<List<LeaderboardViewModel>> GetCurrentLeaderboardAsync();
        Task<LeaderboardViewModel> GetYourCurrentRankingAsync(Guid userId);
        Task<List<LeaderboardViewModel>> GetTop10LeaderboardAsync();

    }
}
