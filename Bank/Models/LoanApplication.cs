using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum LoanType
    {
        Personal,
        Home,
        Auto,
        Education,
        Business
    }

    public enum LoanApplicationStatus
    {
        Draft,
        Submitted,
        UnderReview,
        DocumentsRequired,
        Approved,
        Rejected,
        Disbursed,
        Cancelled
    }

    public class LoanApplication
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string ApplicationNumber { get; set; }

        public LoanType LoanType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RequestedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ApprovedAmount { get; set; }

        public int TermMonths { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? InterestRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonthlyPayment { get; set; }

        [MaxLength(1000)]
        public string Purpose { get; set; }

        [MaxLength(100)]
        public string EmployerName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonthlyIncome { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonthlyExpenses { get; set; }

        public LoanApplicationStatus Status { get; set; } = LoanApplicationStatus.Draft;

        [MaxLength(1000)]
        public string StatusReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SubmittedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime? DisbursedAt { get; set; }

        // Applicant
        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Disbursement account
        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }

        // If approved, links to Loan
        public int? LoanId { get; set; }
        public virtual Loan Loan { get; set; }
    }
}
