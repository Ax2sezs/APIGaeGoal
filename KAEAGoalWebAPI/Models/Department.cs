using System;

namespace KAEAGoalWebAPI.Models
{
    public class Department
    {
        public Guid DepartmentID { get; set; }
        public string Site { get; set; }
        public string DepartmentCode {get;set;}
        public string DepartmentName { get; set; }
    }
}
