using System;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class User
    {
        [Key]
        public Guid A_USER_ID { get; set; }

        public int AU_Employee_ID { get; set; }

        [StringLength(50)]
        public string LOGON_NAME { get; set; }

        [StringLength(250)]
        public string? USER_PASSWORD { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? DisplayName { get; set; }
        public string? ImageUrls { get; set; }

        [StringLength (250)]
        public string? BranchCode { get; set; }

        [StringLength(250)]
        public string? Branch { get; set; }
        public string? DepartmentCode { get; set; }
        public string? Department { get; set; }

        public Guid CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid UpdatedBy { get; set; } 
        public DateTime UpdatedOn { get; set; }
        public int? StateCode { get; set; }
        public int? DeletionStateCode { get; set; }
        public Guid VersionNumber { get; set; }
        public int? IsBkk { get; set; }
        public int? IsAdmin { get; set; }

        [StringLength (250)]
        public string? User_Name { get; set; }
        public bool? Isshop { get; set; }
        public bool? Issup { get; set; }
        public Guid? ST_Dept_Id { get; set; }
        public int? IsQSC { get; set; }

        [StringLength (250)]
        public string? USER_EMAIL { get; set; }

        [StringLength (250)]
        public string? User_Position { get; set; }
        public bool isRegister { get; set; }
        public bool isForcePassChange { get; set; }
        public string? Site { get; set; } 
    }
}
