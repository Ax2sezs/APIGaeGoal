using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class Mission
    {
        [Key]
        public Guid MISSION_ID { get; set; }
        public string MISSION_NAME { get; set; }
        public string MISSION_TYPE { get; set; }
        public int Coin_Reward { get; set; }
        public int Mission_Point { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime Expire_Date { get; set; }
        public string Description { get; set; }

        public bool Is_Limited { get; set; }
        public DateTime Created_At { get; set; }

        public ICollection<MissionImage> MISSION_IMAGES { get; set; }
        public int? Accept_limit { get; set; }
        public int? Current_Accept { get; set; }
        public Guid Missioner { get; set; }
        public string Participate_Type { get; set; }
        public int? Winners { get; set; }

        public CodeMission? CodeMission { get; set; }
        public QrCodeMission? QrCodeMission { get; set; }
        public int? MISSION_Buffer { get; set; }
        public int? MISSION_TypeCoin { get; set; }
        public bool? Is_Winners { get; set; }
        public bool? Is_Public { get; set; }
        public int? WinnerSt { get; set; }
        public int? WinnerNd { get; set; }
        public int? WinnerRd { get; set; }
        public int? WinnerStCoin { get; set; }
        public int? WinnerNdCoin { get; set; }
        public int? WinnerRdCoin { get; set; } 

    }
}
