
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class Operator
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string ColorHex { get; set; } = "#000000";
        
        public string? IconPath { get; set; }
        
        
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
