using System;

namespace KAEAGoalWebAPI.Models
{
    public class FEEDLIKE
    {
        public Guid LIKE_ID { get; set; }
        public Guid USER_MISSION_ID { get; set; }
        public Guid MISSION_ID { get; set; }
        public Guid A_USER_ID { get; set; }
        public string? TYPE {  get; set; }
        public bool? IS_LIKE { get; set; }
        public DateTime? CREATED_AT { get; set; }
        public DateTime? UPDATED_AT { get; set; }
    }
}
