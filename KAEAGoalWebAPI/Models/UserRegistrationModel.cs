using System.ComponentModel.DataAnnotations;
using System;

namespace KAEAGoalWebAPI.Models
{
    public class UserRegistrationModel
    {
        public string LOGON_NAME { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? BranchCode { get; set; }
        public string Department { get; set; }
        public string DepartmentCode { get; set; }
        public string? Branch { get; set; }
        public int? StateCode { get; set; }
        public int? DeletionStateCode { get; set; }
        public int? IsBkk { get; set; }
        public int? IsAdmin { get; set; }
        public string? User_Name { get; set; }
        public bool? Isshop { get; set; }
        public bool? Issup { get; set; }
        public int? IsQSC { get; set; }
        public string? USER_EMAIL { get; set; }
        public string? User_Position { get; set; }
    }
}
