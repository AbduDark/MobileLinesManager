using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MobileLinesManager.Models
{
    public enum GroupType
    {
        WithCashWallet,      // بمحافظ كاش
        WithoutCashWallet,   // بدون محافظ كاش
        Suspended,           // موقوفة
        InMail               // باريد
    }

    public enum GroupStatus
    {
        Active,              // نشطة
        DeliveredToClient,   // مسلمة لعميل
        ReturnedFromClient,  // مستلمة من عميل
        Suspended            // موقوفة
    }

    public class Group
    {
        public int Id { get; set; }

        [Required]
        public int OperatorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public GroupType Type { get; set; } = GroupType.WithoutCashWallet;

        [Required]
        public GroupStatus Status { get; set; } = GroupStatus.Active;

        // حد أقصى للخطوط في المجموعة (افتراضي 50)
        public int MaxLinesCount { get; set; } = 50;

        // تاريخ الصلاحية للمجموعات بمحافظ كاش (60 يوم)
        public DateTime? ValidityDate { get; set; }

        // تاريخ آخر تجديد للصلاحية
        public DateTime? LastRenewalDate { get; set; }

        // عدد أيام الصلاحية (60 يوم للمجموعات بمحافظ كاش)
        public int? ValidityDays { get; set; } = 60;

        // عدد الأيام قبل انتهاء الصلاحية للتنبيه
        public int AlertDaysBeforeExpiry { get; set; } = 7;

        // معلومات التسليم للعميل
        public string? DeliveredToClientName { get; set; }
        
        public DateTime? DeliveryDate { get; set; }
        
        public DateTime? ExpectedReturnDate { get; set; }

        // ملاحظات
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        // التواريخ
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }

        // العلاقات
        public virtual Operator Operator { get; set; } = null!;
        
        public virtual ICollection<Line> Lines { get; set; } = new List<Line>();

        // خصائص محسوبة
        public int CurrentLinesCount => Lines?.Count ?? 0;
        
        public bool IsFull => CurrentLinesCount >= MaxLinesCount;
        
        public bool HasCashWallet => Type == GroupType.WithCashWallet;
        
        public bool IsExpiringSoon
        {
            get
            {
                if (!HasCashWallet || !ValidityDate.HasValue)
                    return false;
                
                var daysUntilExpiry = (ValidityDate.Value - DateTime.Now).Days;
                return daysUntilExpiry <= AlertDaysBeforeExpiry && daysUntilExpiry >= 0;
            }
        }
        
        public bool IsExpired
        {
            get
            {
                if (!HasCashWallet || !ValidityDate.HasValue)
                    return false;
                
                return ValidityDate.Value < DateTime.Now;
            }
        }
        
        public bool IsDeliveryOverdue
        {
            get
            {
                if (Status != GroupStatus.DeliveredToClient || !ExpectedReturnDate.HasValue)
                    return false;
                
                return ExpectedReturnDate.Value < DateTime.Now;
            }
        }

        public int DaysUntilExpiry
        {
            get
            {
                if (!HasCashWallet || !ValidityDate.HasValue)
                    return 0;
                
                return Math.Max(0, (ValidityDate.Value - DateTime.Now).Days);
            }
        }
    }
}
