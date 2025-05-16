using KAEAGoalWebAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KAEAGoalWebAPI.Services
{
    public interface IRewardService
    {
        Task<string> CreateRewardAsync(CreateRewardModel model);
        Task<string> UpdateRewardAsync(Guid rewardId, CreateRewardModel model);
        Task<List<RewardViewModel>> GetAllRewardAsync();
        Task<string> RedeemRewardAsync(Guid userId, RedeemRewardModel model);
        Task<List<UserRewardViewModel>> GetUserRewardAsync(Guid userId);
        Task<string> ChangeStatusUserRewardAsync(Guid userId, ChangeStatusUserReward model);
        Task<List<UserRewardViewModel>> GetAllUserRewardsAsync();
        Task<List<RewardCategoryViewModel>> GetAllRewardCategoryAsync();
    }
}
