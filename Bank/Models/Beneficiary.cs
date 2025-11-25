using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum BeneficiaryType
    {
        Internal,  // Same bank
        External,  // Other bank
        Utility    // Bill payment
    }

    public class Beneficiary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Nickname { get; set; }

        [Required]
        public BeneficiaryType Type { get; set; }

        // For bank transfers
        [MaxLength(20)]
        public string AccountNumber { get; set; }

        [MaxLength(100)]
        public string BankName { get; set; }

        [MaxLength(20)]
        public string RoutingNumber { get; set; }

        // For utility payments
        [MaxLength(100)]
        public string UtilityProvider { get; set; }

        [MaxLength(50)]
        public string CustomerAccountNumber { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Phone]
        [MaxLength(20)]
        public string Phone { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }

        // Owner account
        public int AccountId { get; set; }
        public virtual Account Account { get; set; }

        // Navigation
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<ScheduledPayment> ScheduledPayments { get; set; } = new List<ScheduledPayment>();
    }
}
