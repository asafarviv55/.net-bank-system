using Microsoft.EntityFrameworkCore;

namespace Bank.Services
{
    public interface IAccountService
    {
        Task<Account> CreateAccountAsync(int userId, AccountType type, string name);
        Task<Account> GetAccountByIdAsync(int accountId);
        Task<Account> GetAccountByNumberAsync(string accountNumber);
        Task<IEnumerable<Account>> GetUserAccountsAsync(int userId);
        Task<decimal> GetTotalBalanceAsync(int userId);
        Task<bool> DepositAsync(int accountId, decimal amount, string description);
        Task<bool> WithdrawAsync(int accountId, decimal amount, string description);
        Task<bool> TransferAsync(int fromAccountId, int toAccountId, decimal amount, string description);
    }

    public class AccountService : IAccountService
    {
        private readonly BankContext _context;

        public AccountService(BankContext context)
        {
            _context = context;
        }

        public async Task<Account> CreateAccountAsync(int userId, AccountType type, string name)
        {
            var accountNumber = GenerateAccountNumber();

            var account = new Account
            {
                AccountNumber = accountNumber,
                Type = type,
                AccountName = name,
                Balance = 0,
                AvailableBalance = 0,
                UserId = userId,
                IsActive = true
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return account;
        }

        public async Task<Account> GetAccountByIdAsync(int accountId)
        {
            return await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId);
        }

        public async Task<Account> GetAccountByNumberAsync(string accountNumber)
        {
            return await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }

        public async Task<IEnumerable<Account>> GetUserAccountsAsync(int userId)
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive)
                .OrderBy(a => a.Type)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalBalanceAsync(int userId)
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive)
                .SumAsync(a => a.Balance);
        }

        public async Task<bool> DepositAsync(int accountId, decimal amount, string description)
        {
            if (amount <= 0) return false;

            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null || !account.IsActive) return false;

            account.Balance += amount;
            account.AvailableBalance += amount;
            account.LastTransactionDate = DateTime.UtcNow;

            var transaction = new Transaction
            {
                ReferenceNumber = GenerateReferenceNumber(),
                Type = TransactionType.Deposit,
                Amount = amount,
                BalanceAfter = account.Balance,
                Description = description ?? "Deposit",
                AccountId = accountId,
                Status = TransactionStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                Category = "Income"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> WithdrawAsync(int accountId, decimal amount, string description)
        {
            if (amount <= 0) return false;

            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null || !account.IsActive) return false;
            if (account.AvailableBalance < amount) return false;

            account.Balance -= amount;
            account.AvailableBalance -= amount;
            account.LastTransactionDate = DateTime.UtcNow;

            var transaction = new Transaction
            {
                ReferenceNumber = GenerateReferenceNumber(),
                Type = TransactionType.Withdrawal,
                Amount = -amount,
                BalanceAfter = account.Balance,
                Description = description ?? "Withdrawal",
                AccountId = accountId,
                Status = TransactionStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                Category = "Withdrawal"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> TransferAsync(int fromAccountId, int toAccountId, decimal amount, string description)
        {
            if (amount <= 0) return false;
            if (fromAccountId == toAccountId) return false;

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var fromAccount = await _context.Accounts.FindAsync(fromAccountId);
                var toAccount = await _context.Accounts.FindAsync(toAccountId);

                if (fromAccount == null || toAccount == null) return false;
                if (!fromAccount.IsActive || !toAccount.IsActive) return false;
                if (fromAccount.AvailableBalance < amount) return false;

                var referenceNumber = GenerateReferenceNumber();

                // Debit from source
                fromAccount.Balance -= amount;
                fromAccount.AvailableBalance -= amount;
                fromAccount.LastTransactionDate = DateTime.UtcNow;

                var debitTransaction = new Transaction
                {
                    ReferenceNumber = referenceNumber,
                    Type = TransactionType.Transfer,
                    Amount = -amount,
                    BalanceAfter = fromAccount.Balance,
                    Description = description ?? $"Transfer to {toAccount.AccountNumber}",
                    AccountId = fromAccountId,
                    DestinationAccountId = toAccountId,
                    Status = TransactionStatus.Completed,
                    CompletedAt = DateTime.UtcNow,
                    Category = "Transfer"
                };

                // Credit to destination
                toAccount.Balance += amount;
                toAccount.AvailableBalance += amount;
                toAccount.LastTransactionDate = DateTime.UtcNow;

                var creditTransaction = new Transaction
                {
                    ReferenceNumber = referenceNumber + "-R",
                    Type = TransactionType.Transfer,
                    Amount = amount,
                    BalanceAfter = toAccount.Balance,
                    Description = description ?? $"Transfer from {fromAccount.AccountNumber}",
                    AccountId = toAccountId,
                    Status = TransactionStatus.Completed,
                    CompletedAt = DateTime.UtcNow,
                    Category = "Transfer"
                };

                _context.Transactions.AddRange(debitTransaction, creditTransaction);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                return false;
            }
        }

        private string GenerateAccountNumber()
        {
            return $"ACC{DateTime.UtcNow:yyyyMMdd}{new Random().Next(100000, 999999)}";
        }

        private string GenerateReferenceNumber()
        {
            return $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}
