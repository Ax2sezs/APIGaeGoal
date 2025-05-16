using System;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class ExecuteTextMissionModel
    {
        public Guid MISSION_ID { get; set; }
        public Guid USER_MISSION_ID { get; set; }

        //[StringLength(255)]
        public string Text { get; set; }

    }
}
