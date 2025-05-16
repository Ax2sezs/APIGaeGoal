using System;

namespace KAEAGoalWebAPI.Models
{
    public class UserVideoMission
    {
        public Guid USER_VIDEO_MISSION_ID { get; set; }
        public Guid MISSION_ID { get; set; }
        public Guid A_USER_ID { get; set; }
        public Guid USER_MISSION_ID { get; set; }
        public string VideoUrl { get; set; }
        public DateTime Uploaded_At { get; set; }
        public bool? Approve { get; set; }
        public Guid? Approve_By { get; set; }
        public bool? isReward { get; set; }
        public DateTime? Approve_At { get; set; }
        public bool? IsView { get; set; }

        public User User { get; set; }
        public Mission Mission { get; set; }
        public UserMission UserMission { get; set; }
    }
}
