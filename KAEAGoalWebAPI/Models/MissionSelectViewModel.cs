using System;

namespace KAEAGoalWebAPI.Models
{
    public class MissionSelectViewModel
    {
        public Guid MISSION_ID { get; set; }
        public string MISSION_NAME { get; set; }
        public string MISSION_TYPE { get; set; }
        public string Participate_Type { get; set; }
        public int? MISSION_TypeCoin { get; set; }
        public DateTime CREATED_AT { get; set; }
        public DateTime Start_DATE { get; set; }
        public DateTime Expire_DATE { get; set; }
        public bool? Is_Public { get; set; }

        public int Coin_Reward { get; set; }
        public bool? Is_Winners { get; set; }
        public string description { get; set; }
    }
}
