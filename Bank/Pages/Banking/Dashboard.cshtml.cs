using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bank.Pages.Banking
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ITransactionService _transactionService;
        private readonly IAnalyticsService _analyticsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(
            IAccountService accountService,
            ITransactionService transactionService,
            IAnalyticsService analyticsService,
            UserManager<ApplicationUser> userManager)
        {
            _accountService = accountService;
            _transactionService = transactionService;
            _analyticsService = analyticsService;
            _userManager = userManager;
        }

        public IEnumerable<Account> Accounts { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public IEnumerable<Transaction> RecentTransactions { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            Accounts = await _accountService.GetUserAccountsAsync(user.Id);
            TotalBalance = await _accountService.GetTotalBalanceAsync(user.Id);

            // Get this month's stats
            var now = DateTime.UtcNow;
            var report = await _analyticsService.GenerateMonthlyReportAsync(user.Id, now.Month, now.Year);
            MonthlyIncome = report?.TotalIncome ?? 0;
            MonthlyExpenses = report?.TotalExpenses ?? 0;

            // Get recent transactions from all accounts
            var allTransactions = new List<Transaction>();
            foreach (var account in Accounts)
            {
                var transactions = await _transactionService.GetTransactionsAsync(account.Id);
                allTransactions.AddRange(transactions);
            }

            RecentTransactions = allTransactions
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToList();

            return Page();
        }
    }
}
