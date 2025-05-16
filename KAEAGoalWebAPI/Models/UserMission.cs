using System;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class UserMission
    {
        [Key]
        public Guid USER_MISSION_ID { get; set; }
        public Guid A_USER_ID { get; set; } // Foreign Key to User table
        public Guid MISSION_ID { get; set; } // Foreign Key to Mission table
        public string Verification_Status { get; set; }
        public DateTime Accepted_Date { get; set; }
        public DateTime? Submitted_At { get; set; }
        public DateTime? Completed_Date { get; set; }
        public bool Is_Collect { get; set; }

        public Mission Mission { get; set; }
        public User User { get; set; }
        public string? Accepted_Desc { get; set; }
    }
}
