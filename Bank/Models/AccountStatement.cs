using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public enum StatementFormat
    {
        PDF,
        CSV,
        Excel
    }

    public enum StatementPeriod
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly,
        Custom
    }

    public class AccountStatement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string StatementNumber { get; set; }

        public StatementPeriod Period { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCredits { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDebits { get; set; }

        public int TransactionCount { get; set; }

        public StatementFormat Format { get; set; } = StatementFormat.PDF;

        public string FilePath { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DownloadedAt { get; set; }

        // Account
        public int AccountId { get; set; }
        public virtual Account Account { get; set; }
    }
}
