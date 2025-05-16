using System;

namespace KAEAGoalWebAPI.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }

        public Guid A_USER_ID { get; set; }
        public User USERS { get; set; }
    }
}
