using System;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public enum CoinType
    {
        KaeaCoin,
        ThankCoin,
        MissionPoint 
    }
    public class CoinTransaction
    {
        [Key]
        public Guid COIN_TRANSACTION_ID { get; set; }
        public int Amount { get; set; }
        public DateTime Transaction_Date { get; set; }

        [StringLength(50)]
        public string Transaction_Type { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
        
        public Guid A_USER_ID { get; set; }
        public Guid? Giver_User_ID { get; set; }
        public Guid? Receiver_User_ID { get; set; }

        public CoinType Coin_Type { get; set; }
    }
}
