using System;

namespace KAEAGoalWebAPI.Models
{
    public class ApproveQRCodeMissionModel
    {
        public Guid UserQRCodeMissionId { get; set; }
        public bool Approve { get; set; }
        public string Accepted_Desc { get; set; }
    }
}
