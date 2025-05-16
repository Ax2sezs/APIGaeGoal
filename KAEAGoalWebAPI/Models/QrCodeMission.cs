using System;
using System.ComponentModel.DataAnnotations;

namespace KAEAGoalWebAPI.Models
{
    public class QrCodeMission
    {
        [Key]
        public Guid QR_MISSION_ID { get; set; }
        public Guid MISSION_ID { get; set; }
        public string QRCode { get; set; }
        public string QRCodeText { get; set; }

        public Mission Mission { get; set; }
    }
}
