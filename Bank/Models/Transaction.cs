using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Transfer,
        BillPayment,
        LoanPayment,
        Interest,
        Fee
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }

    public class Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReferenceNumber { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        // Category for spending analytics
        [MaxLength(50)]
        public string Category { get; set; }

        // Source account
        public int AccountId { get; set; }
        public virtual Account Account { get; set; }

        // Destination account (for transfers)
        public int? DestinationAccountId { get; set; }
        public virtual Account DestinationAccount { get; set; }

        // Beneficiary (for external transfers)
        public int? BeneficiaryId { get; set; }
        public virtual Beneficiary Beneficiary { get; set; }
    }
}
