using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum CardType
    {
        Debit,
        Credit,
        Prepaid
    }

    public enum CardStatus
    {
        Active,
        Blocked,
        Frozen,
        Expired,
        Cancelled
    }

    public class Card
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(16)]
        public string CardNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string CardholderName { get; set; }

        public CardType Type { get; set; }

        [MaxLength(3)]
        public string CVV { get; set; }

        public DateTime ExpiryDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CreditLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AvailableCredit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyWithdrawalLimit { get; set; } = 5000;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyTransactionLimit { get; set; } = 10000;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OnlineTransactionLimit { get; set; } = 5000;

        public bool OnlinePaymentsEnabled { get; set; } = true;

        public bool InternationalPaymentsEnabled { get; set; } = false;

        public bool ContactlessEnabled { get; set; } = true;

        public CardStatus Status { get; set; } = CardStatus.Active;

        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;

        public DateTime? BlockedAt { get; set; }

        [MaxLength(500)]
        public string BlockReason { get; set; }

        // PIN (hashed)
        [MaxLength(500)]
        public string PINHash { get; set; }

        public int PINAttempts { get; set; } = 0;

        public DateTime? LastPINAttempt { get; set; }

        // Linked account
        public int AccountId { get; set; }
        public virtual Account Account { get; set; }
    }
}
