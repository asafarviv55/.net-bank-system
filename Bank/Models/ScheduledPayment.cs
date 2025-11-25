using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum PaymentFrequency
    {
        OneTime,
        Daily,
        Weekly,
        BiWeekly,
        Monthly,
        Quarterly,
        Yearly
    }

    public enum ScheduledPaymentStatus
    {
        Active,
        Paused,
        Completed,
        Cancelled
    }

    public class ScheduledPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public PaymentFrequency Frequency { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime? NextExecutionDate { get; set; }

        public DateTime? LastExecutedAt { get; set; }

        public int ExecutionCount { get; set; } = 0;

        public int? MaxExecutions { get; set; }

        public ScheduledPaymentStatus Status { get; set; } = ScheduledPaymentStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Source account
        public int AccountId { get; set; }
        public virtual Account Account { get; set; }

        // Destination (beneficiary)
        public int? BeneficiaryId { get; set; }
        public virtual Beneficiary Beneficiary { get; set; }

        // Or internal transfer
        public int? DestinationAccountId { get; set; }
        public virtual Account DestinationAccount { get; set; }
    }
}
