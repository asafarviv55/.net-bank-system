using Microsoft.EntityFrameworkCore;

namespace Bank.Services
{
    public interface IBillPaymentService
    {
        Task<BillPayment> CreateBillPaymentAsync(int accountId, string providerName, string customerAccountNumber, BillCategory category, decimal amount, DateTime dueDate);
        Task<bool> PayBillAsync(int billPaymentId);
        Task<IEnumerable<BillPayment>> GetBillPaymentsAsync(int accountId, BillPaymentStatus? status = null);
        Task<IEnumerable<BillPayment>> GetUpcomingBillsAsync(int accountId, int daysAhead = 30);
        Task<bool> CancelBillPaymentAsync(int billPaymentId);
    }

    public class BillPaymentService : IBillPaymentService
    {
        private readonly BankContext _context;
        private readonly IAccountService _accountService;

        public BillPaymentService(BankContext context, IAccountService accountService)
        {
            _context = context;
            _accountService = accountService;
        }

        public async Task<BillPayment> CreateBillPaymentAsync(int accountId, string providerName, string customerAccountNumber, BillCategory category, decimal amount, DateTime dueDate)
        {
            var bill = new BillPayment
            {
                ReferenceNumber = GenerateReferenceNumber(),
                ProviderName = providerName,
                CustomerAccountNumber = customerAccountNumber,
                Category = category,
                Amount = amount,
                DueDate = dueDate,
                AccountId = accountId,
                Status = BillPaymentStatus.Pending
            };

            _context.BillPayments.Add(bill);
            await _context.SaveChangesAsync();

            return bill;
        }

        public async Task<bool> PayBillAsync(int billPaymentId)
        {
            var bill = await _context.BillPayments
                .Include(b => b.Account)
                .FirstOrDefaultAsync(b => b.Id == billPaymentId);

            if (bill == null || bill.Status != BillPaymentStatus.Pending)
                return false;

            var totalAmount = bill.Amount + (bill.ServiceFee ?? 0);

            if (bill.Account.AvailableBalance < totalAmount)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                bill.Status = BillPaymentStatus.Processing;
                await _context.SaveChangesAsync();

                // Withdraw from account
                bill.Account.Balance -= totalAmount;
                bill.Account.AvailableBalance -= totalAmount;
                bill.Account.LastTransactionDate = DateTime.UtcNow;

                // Create transaction record
                var txn = new Transaction
                {
                    ReferenceNumber = bill.ReferenceNumber,
                    Type = TransactionType.BillPayment,
                    Amount = -totalAmount,
                    BalanceAfter = bill.Account.Balance,
                    Description = $"Bill payment: {bill.ProviderName}",
                    AccountId = bill.AccountId,
                    Status = TransactionStatus.Completed,
                    CompletedAt = DateTime.UtcNow,
                    Category = bill.Category.ToString()
                };

                _context.Transactions.Add(txn);

                bill.Status = BillPaymentStatus.Paid;
                bill.PaidAt = DateTime.UtcNow;
                bill.TransactionId = txn.Id;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                bill.Status = BillPaymentStatus.Failed;
                await _context.SaveChangesAsync();
                return false;
            }
        }

        public async Task<IEnumerable<BillPayment>> GetBillPaymentsAsync(int accountId, BillPaymentStatus? status = null)
        {
            var query = _context.BillPayments.Where(b => b.AccountId == accountId);

            if (status.HasValue)
                query = query.Where(b => b.Status == status.Value);

            return await query
                .OrderByDescending(b => b.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BillPayment>> GetUpcomingBillsAsync(int accountId, int daysAhead = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

            return await _context.BillPayments
                .Where(b => b.AccountId == accountId &&
                           b.Status == BillPaymentStatus.Pending &&
                           b.DueDate <= cutoffDate)
                .OrderBy(b => b.DueDate)
                .ToListAsync();
        }

        public async Task<bool> CancelBillPaymentAsync(int billPaymentId)
        {
            var bill = await _context.BillPayments.FindAsync(billPaymentId);

            if (bill == null || bill.Status != BillPaymentStatus.Pending)
                return false;

            bill.Status = BillPaymentStatus.Cancelled;
            await _context.SaveChangesAsync();

            return true;
        }

        private string GenerateReferenceNumber()
        {
            return $"BILL{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}
