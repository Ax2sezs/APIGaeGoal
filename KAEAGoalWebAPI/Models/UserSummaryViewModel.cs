using System;

namespace KAEAGoalWebAPI.Models
{
    public class UserSummaryViewModel
    {
        public Guid A_USER_ID { get; set; }
        public string LOGON_NAME { get; set; }
        public string USER_NAME { get; set; }
        public string BranchCode { get; set; }
        public string Department { get; set; }

    }
}
