using System;

namespace KAEAGoalWebAPI.Models
{
    public class ApprovePhotoMissionModel
    {
        public Guid USER_PHOTO_MISSION_ID { get; set; }
        public bool Approve { get; set; }
        public string Accepted_Desc { get; set; }
        public bool? Is_View { get; set; }
    }

    public class ApproveVideoMissionModel
    {
        public Guid USER_VIDEO_MISSION_ID { get; set; }
        public bool Approve { get; set; }
        public string Accepted_Desc { get; set; }
        public bool? Is_View { get; set; }
    }
}
