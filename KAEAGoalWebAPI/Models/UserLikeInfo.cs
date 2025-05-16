using System;

namespace KAEAGoalWebAPI.Models
{
    public class UserLikeInfo
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; }
        public string ProfileImageUrl { get; set; }
        public string bracnhCode { get; set; }
        public string department { get; set; }
    }
}
