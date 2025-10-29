
using System;
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class AssignmentLog
    {
        public int Id { get; set; }
        
        [Required]
        public int LineId { get; set; }
        
        public int? FromUserId { get; set; }
        
        public int? ToUserId { get; set; }
        
        [Required]
        public DateTime AssignedAt { get; set; }
        
        public DateTime? ExpectedReturnDate { get; set; }
        
        public DateTime? ReturnedAt { get; set; }
        
        public string Status { get; set; } // Pending / Returned / Overdue / Cancelled
        
        public string Notes { get; set; }
        
        public virtual Line Line { get; set; }
        
        public virtual User FromUser { get; set; }
        
        public virtual User ToUser { get; set; }
    }
}
