using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bank.Pages.Banking
{
    [Authorize]
    public class TransactionsModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ITransactionService _transactionService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionsModel(
            IAccountService accountService,
            ITransactionService transactionService,
            UserManager<ApplicationUser> userManager)
        {
            _accountService = accountService;
            _transactionService = transactionService;
            _userManager = userManager;
        }

        public Account Account { get; set; }
        public IEnumerable<Transaction> Transactions { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Category { get; set; }
        public string SearchTerm { get; set; }
        public IEnumerable<string> Categories { get; set; } = new[]
        {
            "Groceries", "Utilities", "Transportation", "Entertainment",
            "Dining", "Healthcare", "Shopping", "Travel", "Education",
            "Transfer", "Income", "Loan", "Other"
        };

        public async Task<IActionResult> OnGetAsync(int id, DateTime? fromDate, DateTime? toDate, string category, string search)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            Account = await _accountService.GetAccountByIdAsync(id);

            if (Account == null || Account.UserId != user.Id)
            {
                return RedirectToPage("Dashboard");
            }

            FromDate = fromDate;
            ToDate = toDate;
            Category = category;
            SearchTerm = search;

            if (!string.IsNullOrEmpty(search))
            {
                Transactions = await _transactionService.SearchTransactionsAsync(id, search);
            }
            else
            {
                Transactions = await _transactionService.GetTransactionsAsync(id, fromDate, toDate, category);
            }

            return Page();
        }
    }
}
