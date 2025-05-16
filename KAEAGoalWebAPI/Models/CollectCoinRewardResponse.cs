using System;

namespace KAEAGoalWebAPI.Models
{
    public class CollectCoinRewardResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public int RewardAmount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
