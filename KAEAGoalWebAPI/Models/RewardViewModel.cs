using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class RewardViewModel
    {
        public Guid Reward_Id { get; set; }
        public string Reward_Name { get; set; }
        public string Reward_Description { get; set; }
        public int Reward_price { get; set; }
        public int Reward_quantity { get; set; }
        public List<string> Reward_Image { get; set; } = new List<string>();
        public Guid REWARDCate_Id { get; set; }
        public string REWARDSCate_Name { get; set; } // เพิ่มชื่อหมวดหมู่
        public int Reward_Total { get; set; }
        public int Reward_TotalRedeem { get; set; }
        public string? REWARDSCate_NameEn { get; set; }
    }
    public class RewardCategoryViewModel
    { 
        public Guid REWARDSCate_Id { get; set; }
        public string REWARDSCate_Name { get; set; }
        public string? REWARDSCate_NameEn { get; set; }

        

    }
}
