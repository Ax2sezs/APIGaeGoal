using System.Collections.Generic;
using System;

namespace KAEAGoalWebAPI.Models
{
    public class ApproveTextViewModel
    {
        public Guid USER_TEXT_MISSION_ID { get; set; }
        public Guid A_USER_ID { get; set; }
        public Guid MISSION_ID { get; set; }
        public Guid USER_MISSION_ID { get; set; }
        public string LOGON_NAME { get; set; }
        public string USER_NAME { get; set; }
        public string BranchCode { get; set; }
        public string Department { get; set; }
        public string MISSION_NAME { get; set; }
        public DateTime SUBMIT_DATE { get; set; }
        public List<string> TEXT { get; set; }
        public bool? Approve { get; set; }
        public DateTime? Approve_DATE { get; set; }
        public Guid? Approve_By { get; set; }
        public string? Approve_By_NAME { get; set; }
        public bool? Is_View { get; set; }
        public string? Reject_Des { get; set; }


    }
}
