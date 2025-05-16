using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace KAEAGoalWebAPI.Models
{
    public class CreateMissionModel
    {
        public string MISSION_NAME { get; set; }
        public string MISSION_TYPE { get; set; }
        public int Coin_Reward { get; set; }
        public int Mission_Point { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime Expire_Date { get; set; }
        public string Description { get; set; }
        public List<IFormFile>? Images { get; set; }
        public List<string>? ImageUrls { get; set; }
        public string? Code_Mission_Code { get; set; }
        //public string? QRCode { get; set; }
        public bool Is_Limited { get; set; }
        public int Accept_limit { get; set; }
        public string Participate_Type { get; set; }
        public string? Department { get; set; }
        public string? Site { get; set; } 
        public int? MISSION_Buffer { get; set; }
        public int? MISSION_TypeCoin { get; set; }
        public bool Is_Public { get; set; }
        public int? WinnerSt { get; set; }
        public int? WinnerNd { get; set; }
        public int? WinnerRd { get; set; }
        public int? WinnerStCoin { get; set; }
        public int? WinnerNdCoin { get; set; }
        public int? WinnerRdCoin { get; set; }
        public bool? Is_Winners { get; set; }
        
    }
}
