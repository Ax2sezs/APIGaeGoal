using System;

namespace KAEAGoalWebAPI.Models
{
    public class RewardImage
    {
        public Guid REWARD_IMAGE_ID { get; set; }
        public Guid REWARD_ID { get; set; }
        public string ImageUrls { get; set; }
        public DateTime Uploaded_At { get; set; }

        public Reward Reward { get; set; }
    }
}
