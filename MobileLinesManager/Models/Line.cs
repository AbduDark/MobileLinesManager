using System;
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public class Line
    {
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string SerialNumber { get; set; } = string.Empty;

        // اسم الشخص المرتبط بالخط
        [MaxLength(100)]
        public string AssociatedName { get; set; } = string.Empty;

        // الرقم القومي
        [MaxLength(20)]
        public string NationalId { get; set; } = string.Empty;

        // معرف الخط (يمكن أن يكون رقم تسلسلي أو معرف فريد)
        [MaxLength(50)]
        public string LineIdentifier { get; set; } = string.Empty;

        // معرف محفظة الكاش
        [MaxLength(50)]
        public string CashWalletId { get; set; } = string.Empty;

        public string Status { get; set; } = "Available"; // Available / Assigned / Returned / Blocked / Expired

        public int? AssignedToId { get; set; }

        public DateTime? AssignedAt { get; set; }

        public DateTime? ExpectedReturnDate { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // العلاقات
        public virtual Group Group { get; set; } = null!;

        public virtual User? AssignedTo { get; set; }

        // خصائص محسوبة
        public bool IsAssigned => Status == "Assigned";
        
        public bool IsAvailable => Status == "Available";
    }
}
