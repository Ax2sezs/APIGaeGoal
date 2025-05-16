using System;

namespace KAEAGoalWebAPI.Models
{
    public class UserReward
    {
        public Guid USER_REWARD_ID { get; set; }
        public Guid A_USER_ID { get; set; }
        public Guid REWARD_ID { get; set; }
        public string STATUS { get; set; }
        public DateTime REDEEMED_AT { get; set; }
        public DateTime? OnDelivery_AT { get; set; }
        public DateTime? Delivered_AT { get; set; }
        public DateTime? COLLECT_AT { get; set; }
        public bool? IsCollect { get; set; } 
        public User User { get; set; }
        public Reward Reward { get; set; }
    }
}
