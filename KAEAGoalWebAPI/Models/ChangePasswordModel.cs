namespace KAEAGoalWebAPI.Models
{
    public class ChangePasswordModel
    {
        public string CURRENT_PASSWORD { get; set; }
        public string PASSWORD { get; set; }
        public string CONFIRM_PASSWORD { get; set; }
    }
}
