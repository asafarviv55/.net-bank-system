using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Bank.Pages.Banking
{
    [Authorize]
    public class AnalyticsModel : PageModel
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BankContext _context;

        public AnalyticsModel(
            IAnalyticsService analyticsService,
            UserManager<ApplicationUser> userManager,
            BankContext context)
        {
            _analyticsService = analyticsService;
            _userManager = userManager;
            _context = context;
        }

        public SpendingReport CurrentReport { get; set; }
        public IEnumerable<SpendingReport> HistoricalReports { get; set; }
        public IEnumerable<Budget> Budgets { get; set; }
        public Dictionary<string, decimal> CategoryBreakdown { get; set; }
        public decimal AverageMonthlySpending { get; set; }
        public IEnumerable<SpendingCategory> SpendingCategories { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var now = DateTime.UtcNow;

            // Get current month report
            CurrentReport = await _analyticsService.GenerateMonthlyReportAsync(user.Id, now.Month, now.Year);

            // Parse category breakdown from JSON
            if (!string.IsNullOrEmpty(CurrentReport?.CategoryBreakdown))
            {
                CategoryBreakdown = JsonSerializer.Deserialize<Dictionary<string, decimal>>(CurrentReport.CategoryBreakdown);
            }
            else
            {
                CategoryBreakdown = new Dictionary<string, decimal>();
            }

            // Get historical reports
            HistoricalReports = await _analyticsService.GetReportsAsync(user.Id);

            // Get budgets
            Budgets = await _analyticsService.GetBudgetsAsync(user.Id, now.Month, now.Year);

            // Get average monthly spending
            AverageMonthlySpending = await _analyticsService.GetAverageMonthlySpendingAsync(user.Id);

            // Get spending categories for budget creation
            SpendingCategories = await _context.SpendingCategories
                .Where(c => c.IsSystem || c.UserId == user.Id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateBudgetAsync(string name, string category, decimal limit)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await _analyticsService.CreateBudgetAsync(user.Id, name, category, limit);

            return RedirectToPage();
        }
    }
}
