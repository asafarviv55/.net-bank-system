using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum NotificationType
    {
        Transaction,
        Security,
        Account,
        Card,
        Loan,
        BillPayment,
        System,
        Promotional
    }

    public enum NotificationChannel
    {
        InApp,
        Email,
        SMS,
        Push
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; }

        public NotificationType Type { get; set; }

        public NotificationChannel Channel { get; set; }

        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        [MaxLength(500)]
        public string ActionUrl { get; set; }

        [MaxLength(50)]
        public string ActionText { get; set; }

        // Recipient
        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Related entities (optional)
        public int? TransactionId { get; set; }
        public virtual Transaction Transaction { get; set; }

        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }

        public int? CardId { get; set; }
        public virtual Card Card { get; set; }
    }

    public class NotificationPreference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public NotificationType NotificationType { get; set; }

        public bool EmailEnabled { get; set; } = true;

        public bool SMSEnabled { get; set; } = false;

        public bool PushEnabled { get; set; } = true;

        public bool InAppEnabled { get; set; } = true;
    }
}
