using System;

namespace KAEAGoalWebAPI.Models
{
    public class FeedLikeReq
    {
        public Guid UserMissionId { get; set; }
        public Guid MissionId { get; set; }
        public Guid UserId { get; set; }
        public string? Type { get; set; }
    }
}
