using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum BillCategory
    {
        Electricity,
        Water,
        Gas,
        Internet,
        Phone,
        CableTV,
        Insurance,
        CreditCard,
        Mortgage,
        Rent,
        Other
    }

    public enum BillPaymentStatus
    {
        Pending,
        Processing,
        Paid,
        Failed,
        Cancelled
    }

    public class BillPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReferenceNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProviderName { get; set; }

        [Required]
        [MaxLength(50)]
        public string CustomerAccountNumber { get; set; }

        public BillCategory Category { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ServiceFee { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? PaidAt { get; set; }

        public BillPaymentStatus Status { get; set; } = BillPaymentStatus.Pending;

        [MaxLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Source account
        public int AccountId { get; set; }
        public virtual Account Account { get; set; }

        // Related transaction
        public int? TransactionId { get; set; }
        public virtual Transaction Transaction { get; set; }
    }
}
