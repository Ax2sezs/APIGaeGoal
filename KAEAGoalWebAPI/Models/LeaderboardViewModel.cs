using System;

namespace KAEAGoalWebAPI.Models
{
    public class LeaderboardViewModel
    {
        public string DisplayName { get; set; }
        public Guid A_USER_ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string User_Name { get; set; }
        public int Point { get; set; }
        public string ImageUrls { get; set; }
        public int Rank { get; set; }
        public int RankNo { get; set; }
        public int PointThk { get; set; }
        
        public string DepartmentCode { get; set; }
        public string BranchCode { get; set; }

    }
   
}
