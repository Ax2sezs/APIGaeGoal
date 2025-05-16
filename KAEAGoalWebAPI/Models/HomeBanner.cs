using System;

namespace KAEAGoalWebAPI.Models
{
    public class HomeBanner
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }
    }

}
