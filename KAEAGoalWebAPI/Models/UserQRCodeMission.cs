using System;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class UserQRCodeMission
    {
        [Key]
        public Guid USER_QRCODE_MISSION_ID { get; set; }

        [Required]
        public string QRCode { get; set; }

        public Guid A_USER_ID { get; set; }
        public User User { get; set; }

        public Guid MISSION_ID { get; set; }
        public Mission Mission { get; set; }

        public Guid USER_MISSION_ID { get; set; }
        public UserMission UserMission { get; set; }

        public DateTime Scanned_At { get; set; }
        public bool? Approve { get; set; }
        public Guid? Approved_By { get; set; }
        public DateTime? Approve_At { get; set; }
        public bool? IsReward { get; set; }
    }
}
