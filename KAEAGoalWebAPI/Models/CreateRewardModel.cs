using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace KAEAGoalWebAPI.Models
{
    public class 
        CreateRewardModel
    {
        public string REWARD_NAME { get; set; }
        public int REWARD_PRICE { get; set; }
        public int QUANTITY { get; set; }
        public string? DESCRIPTION { get; set; }
        public List<IFormFile>? ImageFile { get; set; } = new List<IFormFile>();
        public Guid REWARDCate_Id { get; set; } 
    }
}
