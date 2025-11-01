
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class Category
    {
        public int Id { get; set; }
        
        [Required]
        public int OperatorId { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public bool RequiresWallet { get; set; } = false;
        
        public bool RequiresConfirmation { get; set; } = false;
        
        public bool HasExpiry { get; set; } = false;
        
        public int? ExpiryDays { get; set; } = 90;
        
        public int DefaultAlertDaysBeforeExpiry { get; set; } = 30;
        
        public bool AllowAddNumbers { get; set; } = true;
        
        public string Notes { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        public virtual Operator Operator { get; set; } = null!;
        
        public virtual ICollection<Line> Lines { get; set; } = new List<Line>();
    }
}
