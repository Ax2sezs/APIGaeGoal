using System;
using System.Collections.Generic;

namespace KAEAGoalWebAPI.Models
{
    public class UserRewardViewModel
    {
        public Guid User_reward_Id { get; set; }
        public string Reward_Name { get; set; }
        public string USER_NAME { get; set; }
        public string Reward_Description { get; set; }
        public string Reward_Status { get; set; }
        public int Reward_Price { get; set; }
        public DateTime Redeem_Date { get; set; }

        public DateTime? OnDelivery_Date { get; set; }
        public DateTime? Delivered_Date { get; set; }
        public DateTime? Collect_Date { get; set; }
        public List<string> Image { get; set; } = new List<string>(); 
        public Guid REWARDCate_Id { get; set; }
        public string REWARDCate_Name { get; set; } // เพิ่มชื่อหมวดหมู่
        public string User_Firstname { get; set; } // เพิ่มชื่อหมวดหมู่
        public string User_SurName { get; set; } // เพิ่มชื่อหมวดหมู่
        public string DepartmentCode { get; set; } // เพิ่มชื่อหมวดหมู่
        public string Department  { get; set; } // เพิ่มชื่อหมวดหมู่

    }

    public class UserRewardViewModelExcel
    { 
        public string Reward_Name { get; set; }
        public string Reward_Description { get; set; }
        public string Reward_Status { get; set; }
        public int Reward_Price { get; set; }
        public string Redeem_Date { get; set; }
        public string? Collect_Date { get; set; }  
        public string REWARDCate_Name { get; set; } // เพิ่มชื่อหมวดหมู่
        public string USER_NAME { get; set; }
        public string User_Firstname { get; set; } // เพิ่มชื่อหมวดหมู่
        public string User_Lastname { get; set; } // เพิ่มชื่อหมวดหมู่
        public string Department { get; set; } // เพิ่มชื่อหมวดหมู่
        public string DepartmentCode { get; set; }

    }
}
