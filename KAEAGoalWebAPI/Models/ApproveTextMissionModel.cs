using System;

namespace KAEAGoalWebAPI.Models
{
    public class ApproveTextMissionModel
    {
        public Guid USER_TEXT_MISSION_ID { get; set; }
        public bool Approve { get; set; }
        public string Accepted_Desc { get; set; }
        public bool Is_View { get; set; }
    }
}
