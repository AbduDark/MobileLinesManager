
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class AlertRule
    {
        public int Id { get; set; }
        
        public int? CategoryId { get; set; }
        
        public int DaysBeforeExpiry { get; set; } = 30;
        
        public bool Enabled { get; set; } = true;
        
        [Required]
        public string AlertType { get; set; } // Expiry, AssignmentDue, NotReturned
        
        public virtual Category Category { get; set; }
    }
}
