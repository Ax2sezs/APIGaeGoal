using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KAEAGoalWebAPI.Models
{
    public class CodeMission
    {
        [Key]
        public Guid CodeMissionID { get; set; }
        public Guid MISSION_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Code_Mission_Code { get; set; }

        public Mission Mission { get; set; }
    }
}
