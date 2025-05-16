using System.ComponentModel.DataAnnotations;
using System;

namespace KAEAGoalWebAPI.Models
{
    public class UserCoinTransactionViewModel
    {
        public Guid COIN_TRANSACTION_ID { get; set; }
        public int Amount { get; set; }
        public DateTime Transaction_Date { get; set; }
        public string Transaction_Type { get; set; }

        public string Description { get; set; }

        //public Guid A_USER_ID { get; set; }
        //public Guid? Give_User_ID { get; set; }

        public CoinType Coin_Type { get; set; }
    }
}
