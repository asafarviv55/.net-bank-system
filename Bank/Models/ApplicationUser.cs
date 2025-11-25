using Microsoft.AspNetCore.Identity;

namespace Bank.Models
{
    public class ApplicationUser : IdentityUser<int>
    {



        public List<Loan> Loans { get; set; }

        public List<PassBackOperation> passBackOperations { get; set; }


    }
}
