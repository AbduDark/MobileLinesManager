
using System;
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class AuditTrail
    {
        public int Id { get; set; }
        
        [Required]
        public string EntityName { get; set; } = string.Empty;
        
        [Required]
        public string EntityType { get; set; } = string.Empty;
        
        public int EntityId { get; set; }
        
        [Required]
        public string Action { get; set; } = string.Empty; // Create / Update / Delete
        
        public int? UserId { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string Details { get; set; } = string.Empty;
        
        public string OldValues { get; set; } = string.Empty;
        
        public string NewValues { get; set; } = string.Empty;
        
        public virtual User? User { get; set; }
    }
}
