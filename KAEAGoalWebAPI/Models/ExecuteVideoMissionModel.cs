using System;
using Microsoft.AspNetCore.Http;

namespace KAEAGoalWebAPI.Models
{
    public class ExecuteVideoMissionModel
    {
        public Guid UserMissionId { get; set; }
        public Guid MissionId { get; set; }
        public IFormFile VideoFile { get; set; }
    }
}
