using Microsoft.EntityFrameworkCore;

namespace Bank.Services
{
    public interface ICurrencyExchangeService
    {
        Task<CurrencyExchange> ExchangeCurrencyAsync(int userId, int sourceAccountId, int? destinationAccountId, string fromCurrency, string toCurrency, decimal amount);
        Task<CurrencyExchange> GetExchangeByIdAsync(int exchangeId);
        Task<IEnumerable<CurrencyExchange>> GetUserExchangesAsync(int userId);
        Task<ExchangeRate> GetExchangeRateAsync(string baseCurrency, string targetCurrency);
        Task<IEnumerable<ExchangeRate>> GetAllRatesAsync();
        Task<decimal> CalculateExchangeAmountAsync(string fromCurrency, string toCurrency, decimal amount);
        Task<bool> UpdateExchangeRateAsync(string baseCurrency, string targetCurrency, decimal rate, decimal buySpread, decimal sellSpread);
    }

    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly BankContext _context;
        private readonly ITransactionService _transactionService;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public CurrencyExchangeService(
            BankContext context,
            ITransactionService transactionService,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _context = context;
            _transactionService = transactionService;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        public async Task<CurrencyExchange> ExchangeCurrencyAsync(
            int userId,
            int sourceAccountId,
            int? destinationAccountId,
            string fromCurrency,
            string toCurrency,
            decimal amount)
        {
            if (amount <= 0)
                return null;

            var sourceAccount = await _context.Accounts.FindAsync(sourceAccountId);
            if (sourceAccount == null || !sourceAccount.IsActive || sourceAccount.UserId != userId)
                return null;

            Account destinationAccount = null;
            if (destinationAccountId.HasValue)
            {
                destinationAccount = await _context.Accounts.FindAsync(destinationAccountId.Value);
                if (destinationAccount == null || !destinationAccount.IsActive)
                    return null;
            }

            // Get exchange rate
            var exchangeRate = await GetExchangeRateAsync(fromCurrency, toCurrency);
            if (exchangeRate == null || !exchangeRate.IsActive)
                return null;

            // Calculate amounts
            var rate = exchangeRate.Rate;
            var toAmount = amount * rate;
            var fee = amount * 0.01m; // 1% fee
            var totalCost = amount + fee;

            // Check if source account has sufficient balance
            if (sourceAccount.AvailableBalance < totalCost)
                return null;

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var referenceNumber = GenerateReferenceNumber();

                var exchange = new CurrencyExchange
                {
                    ReferenceNumber = referenceNumber,
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    FromAmount = amount,
                    ToAmount = toAmount,
                    ExchangeRate = rate,
                    Fee = fee,
                    TotalCost = totalCost,
                    Status = ExchangeStatus.Pending,
                    SourceAccountId = sourceAccountId,
                    DestinationAccountId = destinationAccountId,
                    UserId = userId
                };

                _context.CurrencyExchanges.Add(exchange);
                await _context.SaveChangesAsync();

                // Debit from source account
                sourceAccount.Balance -= totalCost;
                sourceAccount.AvailableBalance -= totalCost;
                sourceAccount.LastTransactionDate = DateTime.UtcNow;

                var debitTransaction = new Transaction
                {
                    ReferenceNumber = $"{referenceNumber}-DEBIT",
                    Type = TransactionType.Fee,
                    Amount = -totalCost,
                    BalanceAfter = sourceAccount.Balance,
                    Description = $"Currency exchange: {amount:N2} {fromCurrency} to {toCurrency}",
                    AccountId = sourceAccountId,
                    Status = TransactionStatus.Completed,
                    CompletedAt = DateTime.UtcNow,
                    Category = "Currency Exchange"
                };

                _context.Transactions.Add(debitTransaction);
                exchange.DebitTransactionId = debitTransaction.Id;

                // Credit to destination account if provided
                if (destinationAccount != null)
                {
                    destinationAccount.Balance += toAmount;
                    destinationAccount.AvailableBalance += toAmount;
                    destinationAccount.LastTransactionDate = DateTime.UtcNow;

                    var creditTransaction = new Transaction
                    {
                        ReferenceNumber = $"{referenceNumber}-CREDIT",
                        Type = TransactionType.Deposit,
                        Amount = toAmount,
                        BalanceAfter = destinationAccount.Balance,
                        Description = $"Currency exchange: {amount:N2} {fromCurrency} to {toCurrency}",
                        AccountId = destinationAccountId.Value,
                        Status = TransactionStatus.Completed,
                        CompletedAt = DateTime.UtcNow,
                        Category = "Currency Exchange"
                    };

                    _context.Transactions.Add(creditTransaction);
                    exchange.CreditTransactionId = creditTransaction.Id;
                }

                exchange.Status = ExchangeStatus.Completed;
                exchange.CompletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                await _notificationService.CreateNotificationAsync(
                    userId,
                    "Currency Exchange Completed",
                    $"Exchanged {amount:N2} {fromCurrency} to {toAmount:N2} {toCurrency}",
                    NotificationType.Transaction,
                    NotificationPriority.Normal
                );

                await _auditService.LogAsync(
                    AuditAction.CurrencyExchange,
                    $"Currency exchange: {amount:N2} {fromCurrency} to {toAmount:N2} {toCurrency}",
                    userId,
                    accountId: sourceAccountId
                );

                return exchange;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                return null;
            }
        }

        public async Task<CurrencyExchange> GetExchangeByIdAsync(int exchangeId)
        {
            return await _context.CurrencyExchanges
                .Include(e => e.SourceAccount)
                .Include(e => e.DestinationAccount)
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == exchangeId);
        }

        public async Task<IEnumerable<CurrencyExchange>> GetUserExchangesAsync(int userId)
        {
            return await _context.CurrencyExchanges
                .Include(e => e.SourceAccount)
                .Include(e => e.DestinationAccount)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<ExchangeRate> GetExchangeRateAsync(string baseCurrency, string targetCurrency)
        {
            return await _context.ExchangeRates
                .FirstOrDefaultAsync(er =>
                    er.BaseCurrency == baseCurrency &&
                    er.TargetCurrency == targetCurrency &&
                    er.IsActive &&
                    (er.ExpiryDate == null || er.ExpiryDate > DateTime.UtcNow));
        }

        public async Task<IEnumerable<ExchangeRate>> GetAllRatesAsync()
        {
            return await _context.ExchangeRates
                .Where(er => er.IsActive && (er.ExpiryDate == null || er.ExpiryDate > DateTime.UtcNow))
                .OrderBy(er => er.BaseCurrency)
                .ThenBy(er => er.TargetCurrency)
                .ToListAsync();
        }

        public async Task<decimal> CalculateExchangeAmountAsync(string fromCurrency, string toCurrency, decimal amount)
        {
            var exchangeRate = await GetExchangeRateAsync(fromCurrency, toCurrency);
            if (exchangeRate == null || !exchangeRate.IsActive)
                return 0;

            return amount * exchangeRate.Rate;
        }

        public async Task<bool> UpdateExchangeRateAsync(
            string baseCurrency,
            string targetCurrency,
            decimal rate,
            decimal buySpread,
            decimal sellSpread)
        {
            var existingRate = await _context.ExchangeRates
                .FirstOrDefaultAsync(er =>
                    er.BaseCurrency == baseCurrency &&
                    er.TargetCurrency == targetCurrency &&
                    er.IsActive);

            if (existingRate != null)
            {
                existingRate.Rate = rate;
                existingRate.BuySpread = buySpread;
                existingRate.SellSpread = sellSpread;
                existingRate.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                var newRate = new ExchangeRate
                {
                    BaseCurrency = baseCurrency,
                    TargetCurrency = targetCurrency,
                    Rate = rate,
                    BuySpread = buySpread,
                    SellSpread = sellSpread,
                    IsActive = true
                };
                _context.ExchangeRates.Add(newRate);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateReferenceNumber()
        {
            return $"EXC{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}
