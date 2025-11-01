using System;
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class Line
    {
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string SerialNumber { get; set; } = string.Empty;

        public string Status { get; set; } = "Available"; // Available / Assigned / Returned / Blocked / Expired / InWallet / NeedsConfirmation

        public int? AssignedToId { get; set; }

        public DateTime? AssignedAt { get; set; }

        public DateTime? ExpectedReturnDate { get; set; }

        public bool HasWallet { get; set; } = false;

        public string WalletId { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual Category Category { get; set; } = null!;

        public virtual User? AssignedTo { get; set; }

        public bool IsAssigned => Status == "Assigned";
    }
}