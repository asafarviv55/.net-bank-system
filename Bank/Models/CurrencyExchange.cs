using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum ExchangeStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }

    public class CurrencyExchange
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReferenceNumber { get; set; }

        [Required]
        [MaxLength(3)]
        public string FromCurrency { get; set; }

        [Required]
        [MaxLength(3)]
        public string ToCurrency { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FromAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ToAmount { get; set; }

        [Column(TypeName = "decimal(10,6)")]
        public decimal ExchangeRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Fee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        public ExchangeStatus Status { get; set; } = ExchangeStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        // Source account (from which currency is debited)
        public int SourceAccountId { get; set; }
        public virtual Account SourceAccount { get; set; }

        // Destination account (to which currency is credited)
        public int? DestinationAccountId { get; set; }
        public virtual Account DestinationAccount { get; set; }

        // User who initiated the exchange
        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Related transactions
        public int? DebitTransactionId { get; set; }
        public virtual Transaction DebitTransaction { get; set; }

        public int? CreditTransactionId { get; set; }
        public virtual Transaction CreditTransaction { get; set; }
    }

    public class ExchangeRate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(3)]
        public string BaseCurrency { get; set; }

        [Required]
        [MaxLength(3)]
        public string TargetCurrency { get; set; }

        [Column(TypeName = "decimal(10,6)")]
        public decimal Rate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal BuySpread { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal SellSpread { get; set; }

        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
