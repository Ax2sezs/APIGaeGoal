using System;

namespace KAEAGoalWebAPI.Models
{
    public class GiveThankCoinModel
    {
        public Guid receiverId { get; set; }
        public int amount { get; set; }
        public string description { get; set; }
    }
}
