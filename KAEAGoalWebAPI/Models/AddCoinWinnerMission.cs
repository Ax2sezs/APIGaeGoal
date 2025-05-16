using System;
using System.Collections.Generic;

namespace KAEAGoalWebAPI.Models
{
    public class AddCoinWinnerMission
    {
        public Guid A_USER_ID { get; set; }
        public List<Guid>? A_USER_ID_list { get; set; }
        public Guid MISSION_ID { get; set; }
        public int Amount { get; set; }
        public int Rank { get; set; }
    }
}
