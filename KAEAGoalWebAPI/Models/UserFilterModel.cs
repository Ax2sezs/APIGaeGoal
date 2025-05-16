namespace KAEAGoalWebAPI.Models
{
    public class UserFilterModel
    {
        public string? USER_NAME { get; set; }
        public string? BranchCode { get; set; }
        public string? displayName { get; set; }
        public string? Department { get; set; }

        public int PageNumber { get; set; } = 1;  // ค่า default หน้าแรก
        public int PageSize { get; set; } = 10;   // ค่า default ขนาดหน้าละ 10
    }
}
