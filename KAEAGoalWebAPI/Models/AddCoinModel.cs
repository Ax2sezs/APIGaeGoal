using System;

namespace KAEAGoalWebAPI.Models
{
    public class AddCoinModel
    {
        public Guid? UserId { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
    }
}
