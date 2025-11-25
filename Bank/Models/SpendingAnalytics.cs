using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public class SpendingCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Description { get; set; }

        [MaxLength(20)]
        public string Color { get; set; }

        [MaxLength(50)]
        public string Icon { get; set; }

        public bool IsSystem { get; set; } = false;

        public int? UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }

    public class Budget
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentSpent { get; set; } = 0;

        public int Month { get; set; }

        public int Year { get; set; }

        public bool AlertEnabled { get; set; } = true;

        public int AlertThresholdPercent { get; set; } = 80;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Owner
        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Account (optional - for specific account budget)
        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }
    }

    public class SpendingReport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalIncome { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalExpenses { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSavings { get; set; }

        // JSON storing category breakdown
        public string CategoryBreakdown { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // User
        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
