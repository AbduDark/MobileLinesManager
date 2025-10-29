
using System;
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class AuditTrail
    {
        public int Id { get; set; }
        
        [Required]
        public string EntityName { get; set; }
        
        [Required]
        public string EntityType { get; set; }
        
        public int EntityId { get; set; }
        
        [Required]
        public string Action { get; set; } // Create / Update / Delete
        
        public int? UserId { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string Details { get; set; }
        
        public string OldValues { get; set; }
        
        public string NewValues { get; set; }
        
        public virtual User User { get; set; }
    }
}
