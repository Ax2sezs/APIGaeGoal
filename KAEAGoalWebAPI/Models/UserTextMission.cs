using System;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class UserTextMission
    {
        public Guid USER_TEXT_MISSION_ID { get; set; }
        public Guid A_USER_ID { get; set; }
        public Guid MISSION_ID { get; set; }
        public Guid USER_MISSION_ID { get; set; }

        [StringLength(255)]
        public string USER_TEXT { get; set; }
        public DateTime Submitted_At { get; set; }
        public bool? Approve { get; set; }
        public Guid? Approve_By { get; set; }
        public DateTime? Approve_At { get; set; }
        public bool? IsReward { get; set; }
        public bool? IsView { get; set; }

        public User User { get; set; }
        public Mission Mission { get; set; }
        public UserMission UserMission { get; set; }
    }
}
