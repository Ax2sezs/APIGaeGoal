using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KAEAGoalWebAPI.Models
{
    public class MissionImage
    {
        [Key]
        public Guid IMAGE_ID { get; set; }
        public Guid MISSION_ID { get; set; }
        public string ImageUrl { get; set; }
        public DateTime Uploaded_Date { get; set; }

        public Mission Mission { get; set; }
    }
}
