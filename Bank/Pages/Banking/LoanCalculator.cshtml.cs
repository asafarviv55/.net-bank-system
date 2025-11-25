using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Bank.Pages.Banking
{
    public class LoanCalculatorModel : PageModel
    {
        private readonly ILoanService _loanService;

        public LoanCalculatorModel(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public bool ShowResults { get; set; }
        public decimal MonthlyPayment { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPayment { get; set; }

        public class InputModel
        {
            [Required]
            public LoanType LoanType { get; set; } = LoanType.Personal;

            [Required]
            [Range(1000, 10000000, ErrorMessage = "Loan amount must be between $1,000 and $10,000,000")]
            public decimal Principal { get; set; } = 10000;

            [Required]
            [Range(0.1, 30, ErrorMessage = "Interest rate must be between 0.1% and 30%")]
            public decimal AnnualRate { get; set; } = 5.99m;

            [Required]
            public int TermMonths { get; set; } = 36;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var (monthly, interest, total) = await _loanService.CalculateLoanAsync(
                Input.Principal,
                Input.AnnualRate,
                Input.TermMonths);

            MonthlyPayment = monthly;
            TotalInterest = interest;
            TotalPayment = total;
            ShowResults = true;

            return Page();
        }
    }
}
