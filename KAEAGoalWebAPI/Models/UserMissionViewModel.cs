using System;
using System.Collections.Generic;

namespace KAEAGoalWebAPI.Models
{
    public class UserMissionViewModel
    {
        public Guid A_USER_ID { get; set; }
        public string LOGON_NAME { get; set; }
        public Guid USER_MISSION_ID { get; set; }
        public Guid MISSION_ID { get; set; }
        public int Coin_Reward { get; set; }
        public int Point_Reward { get; set; }
        public int? MISSION_TypeCoin { get; set; }
        public string Mission_Name { get; set; }
        public string Mission_Type { get; set; }
        public string Description { get; set; }
        public DateTime Expire_Date { get; set; }
        public List<string> Mission_Image { get; set; } = new List<string>();
        public string Verification_Status { get; set; }
        public DateTime? Accepted_Date { get; set; }
        public DateTime? Submitted_At { get; set; }
        public DateTime? Completed_Date { get; set; }
        public bool Is_Collect { get; set; }
        public int Accept_limit { get; set; }
        public int Current_Accept { get; set; }
        public string Accepted_Desc { get; set; }
        public bool? Is_Public { get; set; }
        
    }
}
