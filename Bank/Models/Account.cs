using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum AccountType
    {
        Checking,
        Savings,
        Business
    }

    public class Account
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string AccountNumber { get; set; }

        [Required]
        public AccountType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AvailableBalance { get; set; }

        [MaxLength(100)]
        public string AccountName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastTransactionDate { get; set; }

        // Foreign key
        public int UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Navigation properties
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
        public virtual ICollection<ScheduledPayment> ScheduledPayments { get; set; } = new List<ScheduledPayment>();
    }
}
