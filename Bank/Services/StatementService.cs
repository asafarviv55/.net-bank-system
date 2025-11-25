using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Bank.Services
{
    public interface IStatementService
    {
        Task<AccountStatement> GenerateStatementAsync(int accountId, DateTime startDate, DateTime endDate, StatementFormat format);
        Task<IEnumerable<AccountStatement>> GetStatementsAsync(int accountId);
        Task<AccountStatement> GetStatementAsync(int statementId);
        Task<string> GenerateCSVContentAsync(int accountId, DateTime startDate, DateTime endDate);
        Task<byte[]> GeneratePDFContentAsync(int accountId, DateTime startDate, DateTime endDate);
    }

    public class StatementService : IStatementService
    {
        private readonly BankContext _context;

        public StatementService(BankContext context)
        {
            _context = context;
        }

        public async Task<AccountStatement> GenerateStatementAsync(int accountId, DateTime startDate, DateTime endDate, StatementFormat format)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return null;

            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt <= endDate)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            var openingBalance = await GetBalanceAtDateAsync(accountId, startDate);
            var totalCredits = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var totalDebits = transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
            var closingBalance = openingBalance + totalCredits - totalDebits;

            var statement = new AccountStatement
            {
                StatementNumber = GenerateStatementNumber(),
                Period = DeterminePeriod(startDate, endDate),
                StartDate = startDate,
                EndDate = endDate,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                TotalCredits = totalCredits,
                TotalDebits = totalDebits,
                TransactionCount = transactions.Count,
                Format = format,
                AccountId = accountId
            };

            _context.AccountStatements.Add(statement);
            await _context.SaveChangesAsync();

            return statement;
        }

        public async Task<IEnumerable<AccountStatement>> GetStatementsAsync(int accountId)
        {
            return await _context.AccountStatements
                .Where(s => s.AccountId == accountId)
                .OrderByDescending(s => s.GeneratedAt)
                .ToListAsync();
        }

        public async Task<AccountStatement> GetStatementAsync(int statementId)
        {
            return await _context.AccountStatements
                .Include(s => s.Account)
                .FirstOrDefaultAsync(s => s.Id == statementId);
        }

        public async Task<string> GenerateCSVContentAsync(int accountId, DateTime startDate, DateTime endDate)
        {
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null) return null;

            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt <= endDate)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();

            // Header info
            sb.AppendLine($"Account Statement - {account.AccountNumber}");
            sb.AppendLine($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // CSV header
            sb.AppendLine("Date,Reference,Type,Description,Amount,Balance,Category");

            // Transactions
            foreach (var txn in transactions)
            {
                sb.AppendLine($"{txn.CreatedAt:yyyy-MM-dd HH:mm},{txn.ReferenceNumber},{txn.Type},{EscapeCSV(txn.Description)},{txn.Amount:F2},{txn.BalanceAfter:F2},{txn.Category}");
            }

            // Summary
            sb.AppendLine();
            sb.AppendLine($"Total Credits,{transactions.Where(t => t.Amount > 0).Sum(t => t.Amount):F2}");
            sb.AppendLine($"Total Debits,{transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount)):F2}");
            sb.AppendLine($"Transaction Count,{transactions.Count}");

            return sb.ToString();
        }

        public async Task<byte[]> GeneratePDFContentAsync(int accountId, DateTime startDate, DateTime endDate)
        {
            // For a real implementation, use a PDF library like iTextSharp or QuestPDF
            // This returns the CSV content as bytes as a placeholder
            var csvContent = await GenerateCSVContentAsync(accountId, startDate, endDate);
            return Encoding.UTF8.GetBytes(csvContent ?? string.Empty);
        }

        private async Task<decimal> GetBalanceAtDateAsync(int accountId, DateTime date)
        {
            var lastTransaction = await _context.Transactions
                .Where(t => t.AccountId == accountId && t.CreatedAt < date)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            return lastTransaction?.BalanceAfter ?? 0;
        }

        private StatementPeriod DeterminePeriod(DateTime startDate, DateTime endDate)
        {
            var days = (endDate - startDate).Days;

            if (days <= 1) return StatementPeriod.Daily;
            if (days <= 7) return StatementPeriod.Weekly;
            if (days <= 31) return StatementPeriod.Monthly;
            if (days <= 93) return StatementPeriod.Quarterly;
            if (days <= 366) return StatementPeriod.Yearly;

            return StatementPeriod.Custom;
        }

        private string GenerateStatementNumber()
        {
            return $"STMT{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
