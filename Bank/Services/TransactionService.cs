using Microsoft.EntityFrameworkCore;

namespace Bank.Services
{
    public interface ITransactionService
    {
        Task<IEnumerable<Transaction>> GetTransactionsAsync(int accountId, DateTime? from = null, DateTime? to = null, string category = null);
        Task<Transaction> GetTransactionByIdAsync(int transactionId);
        Task<Transaction> GetTransactionByReferenceAsync(string referenceNumber);
        Task<decimal> GetTotalSpendingByCategoryAsync(int accountId, string category, int month, int year);
        Task<Dictionary<string, decimal>> GetSpendingByCategoryAsync(int accountId, int month, int year);
        Task<IEnumerable<Transaction>> SearchTransactionsAsync(int accountId, string searchTerm);
    }

    public class TransactionService : ITransactionService
    {
        private readonly BankContext _context;

        public TransactionService(BankContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(int accountId, DateTime? from = null, DateTime? to = null, string category = null)
        {
            var query = _context.Transactions
                .Where(t => t.AccountId == accountId)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(t => t.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(t => t.CreatedAt <= to.Value);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category == category);

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        public async Task<Transaction> GetTransactionByIdAsync(int transactionId)
        {
            return await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.DestinationAccount)
                .Include(t => t.Beneficiary)
                .FirstOrDefaultAsync(t => t.Id == transactionId);
        }

        public async Task<Transaction> GetTransactionByReferenceAsync(string referenceNumber)
        {
            return await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.ReferenceNumber == referenceNumber);
        }

        public async Task<decimal> GetTotalSpendingByCategoryAsync(int accountId, string category, int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return await _context.Transactions
                .Where(t => t.AccountId == accountId &&
                           t.Category == category &&
                           t.Amount < 0 &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt < endDate)
                .SumAsync(t => Math.Abs(t.Amount));
        }

        public async Task<Dictionary<string, decimal>> GetSpendingByCategoryAsync(int accountId, int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var spending = await _context.Transactions
                .Where(t => t.AccountId == accountId &&
                           t.Amount < 0 &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt < endDate &&
                           t.Category != null)
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(t => Math.Abs(t.Amount)) })
                .ToListAsync();

            return spending.ToDictionary(x => x.Category, x => x.Total);
        }

        public async Task<IEnumerable<Transaction>> SearchTransactionsAsync(int accountId, string searchTerm)
        {
            return await _context.Transactions
                .Where(t => t.AccountId == accountId &&
                           (t.Description.Contains(searchTerm) ||
                            t.ReferenceNumber.Contains(searchTerm) ||
                            t.Category.Contains(searchTerm)))
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .ToListAsync();
        }
    }
}
