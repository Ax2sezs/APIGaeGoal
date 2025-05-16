using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace KAEAGoalWebAPI.Models
{
    public class ExecutePhotoModel
    {
        public Guid missionId { get; set; }
        //public Guid userId { get; set; }
        public Guid userMissionId { get; set; }
        public List<IFormFile> imageFile { get; set; } = new List<IFormFile>();
    }

    public class ExecuteVideoModel
    {
        public Guid missionId { get; set; }
        //public Guid userId { get; set; }
        public Guid userMissionId { get; set; }
        public  IFormFile  videoFile { get; set; }

    }
}
