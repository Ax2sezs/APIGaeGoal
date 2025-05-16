using System;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class UserCodeMission
    {
        [Key]
        public Guid USER_CODE_MISSION_ID { get; set; }

        [Required]
        public string Code { get; set; }

        public Guid A_USER_ID { get; set; }
        public User User { get; set; }

        public Guid MISSION_ID { get; set; }
        public Mission Mission { get; set; }

        public DateTime Submit_At { get; set; }
    }
}
