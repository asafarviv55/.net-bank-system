using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public class CreditExpense
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        public ApplicationUser owner { get; set; }

        public DateTime next_billing_date { get; set; }

        public string card_number { get; set; }

        public decimal amount { get; set; }

        public string credit_card_holder { get; set; }

        public string credit_company { get; set; }

        public string credit_card_type { get; set; }

    }
}
