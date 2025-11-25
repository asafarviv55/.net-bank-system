using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    public class PassBackOperation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        public ApplicationUser owner { get; set; }

        [ForeignKey("owner")]
        public int ownerID { get; set; }


        public DateTime created_at { get; set; }

        public string reference { get; set; }

        public string action { get; set; }

        public decimal right_balance { get; set; }

        public decimal due_balance { get; set; }

        public decimal account_balance { get; set; }


    }
}
