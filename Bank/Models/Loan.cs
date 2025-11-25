using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public class Loan
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [ForeignKey("ApplicationUser")]
        [MaxLength(450)]
        public string UserId { get; set; }

        public ApplicationUser User { get; set; }

        public decimal current_interest_rate { get; set; }

        public int original_loan_fund { get; set; }

        public DateTime next_payment_date { get; set; }

        public decimal next_payment_amount { get; set; }

    }
}
