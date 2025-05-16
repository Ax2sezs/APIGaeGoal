using System;

namespace KAEAGoalWebAPI.Models
{
    public class UserDetailsModel
    {
        public Guid A_USER_ID { get; set; }
        public string LOGON_NAME { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string ImageUrls { get; set; }
        public string BranchCode { get; set; }
        public string Branch { get; set; }
        public string Department { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int? StateCode { get; set; }
        public int? DeletionStateCode { get; set; }
        public int? IsBkk { get; set; }
        public int? IsAdmin { get; set; }
        public string User_Name { get; set; }
        public bool? Isshop { get; set; }
        public bool? Issup { get; set; }
        public string? USER_EMAIL { get; set; }
        public string? User_Position { get; set; }
        public bool isRegister { get; set; }
        public bool isForcePassChange { get; set; }
        public string? Site { get; set; }
        public int AU_Employee_ID { get; set; }
        public string? DepartmentCode { get; set; }
        
    }
}
