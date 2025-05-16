using System;

namespace KAEAGoalWebAPI.Models
{
    public class UserPhotoMissionImage
    {
        public Guid USER_PHOTO_MISSION_IMAGE_ID { get; set; }
        public Guid USER_PHOTO_MISSION_ID { get; set; }
        public string ImageUrl { get; set; }

        public UserPhotoMission UserPhotoMission { get; set; }
    }

    
}
