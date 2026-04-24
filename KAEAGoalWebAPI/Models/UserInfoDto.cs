using System;

namespace KAEAGoalWebAPI.Models
{
    public class UserInfoDto
    {
        public Guid A_USER_ID { get; set; }
        public string LogonName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BranchCode { get; set; }
        public string Department { get; set; }
        public int? StateCode { get; set; }
    }
}