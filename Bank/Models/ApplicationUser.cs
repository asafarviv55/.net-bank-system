using Microsoft.AspNetCore.Identity;

namespace Bank.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string PostalCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

        public virtual ICollection<PassBackOperation> PassBackOperations { get; set; } = new List<PassBackOperation>();

        public virtual ICollection<LoanApplication> LoanApplications { get; set; } = new List<LoanApplication>();

        public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();

        public virtual ICollection<SpendingReport> SpendingReports { get; set; } = new List<SpendingReport>();
    }
}
