using System;
using System.Collections.Generic;

namespace KAEAGoalWebAPI.Models
{
    public class MissionViewModel
    {
        public Guid MISSION_ID { get; set; }
        public string? MISSION_NAME { get; set; }
        public string? MISSION_TYPE { get; set; }
        public int? Coin_Reward { get; set; }
        public int? Mission_Point { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime? Expire_Date { get; set; }
        public string? Description { get; set; }
        public bool Is_Limited { get; set; }

        // Optionally, add properties for related models like images or code missions
        public List<string> MissionImages { get; set; } = new List<string>();  // URLs of images
        public string CodeMission { get; set; } // If applicable
        public string QrMission { get; set; }
        public int Accept_limit { get; set; }
        public int? Current_Accept { get; set; }
        public string Participate_Type { get; set; }

        public int? MISSION_Buffer { get; set; }
        public int? MISSION_TypeCoin { get; set; }
        public bool? Is_Public { get; set; }
        public int? WinnerSt { get; set; }
        public int? WinnerNd { get; set; }
        public int? WinnerRd { get; set; }
        public int? WinnerStCoin { get; set; }
        public int? WinnerNdCoin { get; set; }
        public int? WinnerRdCoin { get; set; }
    }
}
