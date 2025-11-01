using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class AlertRule
    {
        public int Id { get; set; }

        public int? GroupId { get; set; }

        public int DaysBeforeExpiry { get; set; } = 30;

        public bool Enabled { get; set; } = true;

        [Required]
        public string AlertType { get; set; } = "Expiry"; // Expiry, AssignmentDue, NotReturned

        public virtual Group? Group { get; set; }
    }
}