using System;

namespace KAEAGoalWebAPI.Models
{
    public class ApproveViewModel
    {
        public Guid USER_QR_CODE_MISSION_ID { get; set; }
        public string QRCode { get; set; }
        public Guid A_USER_ID { get; set; }
        public string LOGON_NAME { get; set; }
        public string USER_NAME { get; set; }
        public string BranchCode { get; set; }
        public string Department { get; set; }
        public Guid MISSION_ID { get; set; }
        public string MISSION_NAME { get; set; }
        public Guid USER_MISSION_ID { get; set; }
        public DateTime Scanned_At { get; set; }
        public bool? Approve { get; set; }
        public Guid? Approve_By { get; set; }
        public DateTime? Approve_DATE { get; set; }

    }
}
