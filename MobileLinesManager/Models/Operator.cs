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

        public string IconPath { get; set; } = string.Empty;

        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}