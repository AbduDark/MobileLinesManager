
using System;
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string PasswordHash { get; set; }
        
        [Required]
        public string FullName { get; set; }
        
        [Required]
        public string Role { get; set; } // Admin / Manager / Worker
        
        public string Phone { get; set; }
        
        public string Email { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
