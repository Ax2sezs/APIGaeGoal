using System.Collections.Generic;
using System;

namespace KAEAGoalWebAPI.Models
{
    public class FeedViewModel
    {
        public string Type { get; set; } // "photo", "video", "text"
        public Guid USER_MISSION_ID { get; set; }
        public Guid USER_ID { get; set; }
        public string USER_NAME { get; set; }
        public string Display_NAME { get; set; }
        public string? ImageURL { get; set; }
        public string LOGON_NAME { get; set; }
        public string BranchCode { get; set; }
        public string Department { get; set; }
        public Guid MISSION_ID { get; set; }
        public string MISSION_NAME { get; set; }
        public DateTime? SUBMIT_DATE { get; set; }
        public List<string> CONTENT_URLS { get; set; }
        public int? LIKE_COUNT { get; set; } 
        public bool? IS_LIKE { get; set; }
    }


}
