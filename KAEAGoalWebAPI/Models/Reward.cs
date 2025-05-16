using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class Reward
    {
        public Guid REWARD_ID { get; set; }
        public string REWARD_NAME { get; set; }
        public string? DESCRIPTION { get; set; }
        public int PRICE { get; set; }
        public int QUANTITY { get; set; }
        public ICollection<RewardImage> REWARD_IMAGES { get; set; } 
        
        public Guid REWARDCate_Id { get; set; }
        public Reward_Category REWARD_CATEGORY { get; set; }

    }

    public class Reward_Category
    {
        [Key]
        public Guid REWARDSCate_Id { get; set; }
        public string REWARDSCate_Name { get; set; }
        public string? REWARDSCate_NameEn { get; set; } 

    }
}
