using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Bank.Services
{
    public interface IAnalyticsService
    {
        Task<SpendingReport> GenerateMonthlyReportAsync(int userId, int month, int year);
        Task<IEnumerable<SpendingReport>> GetReportsAsync(int userId);
        Task<Budget> CreateBudgetAsync(int userId, string name, string category, decimal monthlyLimit, int? accountId = null);
        Task<IEnumerable<Budget>> GetBudgetsAsync(int userId, int month, int year);
        Task<bool> UpdateBudgetAsync(int budgetId, decimal monthlyLimit, int alertThreshold);
        Task<bool> DeleteBudgetAsync(int budgetId);
        Task<Dictionary<string, decimal>> GetCategoryTrendsAsync(int userId, int months = 6);
        Task<decimal> GetAverageMonthlySpendingAsync(int userId, int months = 6);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly BankContext _context;

        public AnalyticsService(BankContext context)
        {
            _context = context;
        }

        public async Task<SpendingReport> GenerateMonthlyReportAsync(int userId, int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive)
                .Select(a => a.Id)
                .ToListAsync();

            var transactions = await _context.Transactions
                .Where(t => accounts.Contains(t.AccountId) &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt < endDate)
                .ToListAsync();

            var totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var totalExpenses = transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));

            var categoryBreakdown = transactions
                .Where(t => t.Amount < 0 && !string.IsNullOrEmpty(t.Category))
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount)));

            // Check if report already exists
            var existingReport = await _context.SpendingReports
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Month == month && r.Year == year);

            if (existingReport != null)
            {
                existingReport.TotalIncome = totalIncome;
                existingReport.TotalExpenses = totalExpenses;
                existingReport.NetSavings = totalIncome - totalExpenses;
                existingReport.CategoryBreakdown = JsonSerializer.Serialize(categoryBreakdown);
                existingReport.GeneratedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existingReport;
            }

            var report = new SpendingReport
            {
                Month = month,
                Year = year,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetSavings = totalIncome - totalExpenses,
                CategoryBreakdown = JsonSerializer.Serialize(categoryBreakdown),
                UserId = userId
            };

            _context.SpendingReports.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }

        public async Task<IEnumerable<SpendingReport>> GetReportsAsync(int userId)
        {
            return await _context.SpendingReports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .Take(12)
                .ToListAsync();
        }

        public async Task<Budget> CreateBudgetAsync(int userId, string name, string category, decimal monthlyLimit, int? accountId = null)
        {
            var now = DateTime.UtcNow;

            var budget = new Budget
            {
                Name = name,
                Category = category,
                MonthlyLimit = monthlyLimit,
                Month = now.Month,
                Year = now.Year,
                UserId = userId,
                AccountId = accountId
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            // Calculate current spending
            await UpdateBudgetSpendingAsync(budget);

            return budget;
        }

        public async Task<IEnumerable<Budget>> GetBudgetsAsync(int userId, int month, int year)
        {
            var budgets = await _context.Budgets
                .Where(b => b.UserId == userId && b.Month == month && b.Year == year)
                .ToListAsync();

            // Update current spending for each budget
            foreach (var budget in budgets)
            {
                await UpdateBudgetSpendingAsync(budget);
            }

            return budgets;
        }

        public async Task<bool> UpdateBudgetAsync(int budgetId, decimal monthlyLimit, int alertThreshold)
        {
            var budget = await _context.Budgets.FindAsync(budgetId);
            if (budget == null) return false;

            budget.MonthlyLimit = monthlyLimit;
            budget.AlertThresholdPercent = alertThreshold;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBudgetAsync(int budgetId)
        {
            var budget = await _context.Budgets.FindAsync(budgetId);
            if (budget == null) return false;

            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Dictionary<string, decimal>> GetCategoryTrendsAsync(int userId, int months = 6)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => a.Id)
                .ToListAsync();

            var trends = await _context.Transactions
                .Where(t => accounts.Contains(t.AccountId) &&
                           t.CreatedAt >= startDate &&
                           t.Amount < 0 &&
                           !string.IsNullOrEmpty(t.Category))
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Average = g.Average(t => Math.Abs(t.Amount)) })
                .ToListAsync();

            return trends.ToDictionary(t => t.Category, t => (decimal)t.Average);
        }

        public async Task<decimal> GetAverageMonthlySpendingAsync(int userId, int months = 6)
        {
            var reports = await _context.SpendingReports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .Take(months)
                .ToListAsync();

            if (!reports.Any()) return 0;

            return reports.Average(r => r.TotalExpenses);
        }

        private async Task UpdateBudgetSpendingAsync(Budget budget)
        {
            var startDate = new DateTime(budget.Year, budget.Month, 1);
            var endDate = startDate.AddMonths(1);

            var accountIds = budget.AccountId.HasValue
                ? new List<int> { budget.AccountId.Value }
                : await _context.Accounts
                    .Where(a => a.UserId == budget.UserId)
                    .Select(a => a.Id)
                    .ToListAsync();

            var spent = await _context.Transactions
                .Where(t => accountIds.Contains(t.AccountId) &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt < endDate &&
                           t.Amount < 0 &&
                           t.Category == budget.Category)
                .SumAsync(t => Math.Abs(t.Amount));

            budget.CurrentSpent = spent;
            await _context.SaveChangesAsync();
        }
    }
}
