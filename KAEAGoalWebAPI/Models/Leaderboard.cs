using System;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class Leaderboard
    {
        public Guid LEADERBOARD_ID { get; set; }
        public Guid A_USER_ID { get; set; }
        public int Point { get; set; }
        public int? Rank { get; set; }
        public DateTime MonthYear { get; set; }
        public DateTime Create_at { get; set; }

        public User User { get; set; }
    }
    public class uvw_Leaderboard
    {
        public int RankNo { get; set; }
        [Key]
        public Guid LEADERBOARD_ID { get; set; }
        public Guid A_USER_ID { get; set; }
        public int Point { get; set; }
        public int? Rank { get; set; }
        public DateTime MonthYear { get; set; }
        public DateTime Create_at { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string User_Name { get; set; }
        public string ImageUrls { get; set; }
        public int PointThk { get; set; }

         
        //public User User { get; set; }
    }

}
