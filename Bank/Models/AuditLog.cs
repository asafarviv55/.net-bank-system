using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum AuditAction
    {
        Login,
        Logout,
        PasswordChange,
        AccountCreated,
        AccountUpdated,
        AccountDeleted,
        TransactionCreated,
        TransactionUpdated,
        TransactionCancelled,
        BillPayment,
        LoanApplication,
        LoanApproval,
        CardIssued,
        CardBlocked,
        CardUnblocked,
        SettingsChanged,
        ProfileUpdated,
        BeneficiaryAdded,
        BeneficiaryDeleted,
        CurrencyExchange,
        ScheduledPaymentCreated,
        ScheduledPaymentCancelled,
        SecurityAlert,
        FailedLoginAttempt
    }

    public enum AuditSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public AuditAction Action { get; set; }

        public AuditSeverity Severity { get; set; } = AuditSeverity.Info;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string EntityType { get; set; }

        [MaxLength(50)]
        public string EntityId { get; set; }

        [MaxLength(100)]
        public string IPAddress { get; set; }

        [MaxLength(500)]
        public string UserAgent { get; set; }

        [MaxLength(50)]
        public string SessionId { get; set; }

        // Before and after state for tracking changes (JSON)
        public string OldValues { get; set; }

        public string NewValues { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // User who performed the action (nullable for system actions)
        public int? UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Related account (if applicable)
        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }

        // Related transaction (if applicable)
        public int? TransactionId { get; set; }
        public virtual Transaction Transaction { get; set; }
    }

    public class SecurityEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        public AuditSeverity Severity { get; set; }

        [MaxLength(100)]
        public string IPAddress { get; set; }

        [MaxLength(500)]
        public string UserAgent { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        public bool IsResolved { get; set; } = false;

        public DateTime? ResolvedAt { get; set; }

        [MaxLength(1000)]
        public string ResolutionNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // User affected by the security event
        public int? UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
