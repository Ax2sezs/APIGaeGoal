using System;

namespace KAEAGoalWebAPI.Models
{
    public class IsViewModel
    {
        public string MissionType { get; set; } // "text", "photo", "video"
        public Guid UserMissionId { get; set; }
        public bool IsView { get; set; }
    }

}
