using System;

namespace KAEAGoalWebAPI.Models
{
    public class ExecuteQRCodeModel
    {
        public Guid MissionId { get; set; }
        public Guid UserMissionId { get; set; }
        public string QRCode { get; set; }
    }
}
