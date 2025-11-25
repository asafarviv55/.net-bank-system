using Microsoft.EntityFrameworkCore;

namespace Bank.Services
{
    public interface IScheduledPaymentService
    {
        Task<ScheduledPayment> CreateScheduledPaymentAsync(int accountId, string name, decimal amount, PaymentFrequency frequency, DateTime startDate, int? beneficiaryId = null, int? destinationAccountId = null);
        Task<IEnumerable<ScheduledPayment>> GetScheduledPaymentsAsync(int accountId);
        Task<ScheduledPayment> GetScheduledPaymentAsync(int paymentId);
        Task<bool> PauseScheduledPaymentAsync(int paymentId);
        Task<bool> ResumeScheduledPaymentAsync(int paymentId);
        Task<bool> CancelScheduledPaymentAsync(int paymentId);
        Task<int> ProcessDuePaymentsAsync();
    }

    public class ScheduledPaymentService : IScheduledPaymentService
    {
        private readonly BankContext _context;
        private readonly IAccountService _accountService;
        private readonly IBeneficiaryService _beneficiaryService;

        public ScheduledPaymentService(BankContext context, IAccountService accountService, IBeneficiaryService beneficiaryService)
        {
            _context = context;
            _accountService = accountService;
            _beneficiaryService = beneficiaryService;
        }

        public async Task<ScheduledPayment> CreateScheduledPaymentAsync(int accountId, string name, decimal amount, PaymentFrequency frequency, DateTime startDate, int? beneficiaryId = null, int? destinationAccountId = null)
        {
            if (beneficiaryId == null && destinationAccountId == null)
                throw new ArgumentException("Either beneficiary or destination account must be specified");

            var payment = new ScheduledPayment
            {
                Name = name,
                Amount = amount,
                Frequency = frequency,
                StartDate = startDate,
                NextExecutionDate = startDate,
                AccountId = accountId,
                BeneficiaryId = beneficiaryId,
                DestinationAccountId = destinationAccountId,
                Status = ScheduledPaymentStatus.Active
            };

            _context.ScheduledPayments.Add(payment);
            await _context.SaveChangesAsync();

            return payment;
        }

        public async Task<IEnumerable<ScheduledPayment>> GetScheduledPaymentsAsync(int accountId)
        {
            return await _context.ScheduledPayments
                .Include(s => s.Beneficiary)
                .Include(s => s.DestinationAccount)
                .Where(s => s.AccountId == accountId)
                .OrderBy(s => s.NextExecutionDate)
                .ToListAsync();
        }

        public async Task<ScheduledPayment> GetScheduledPaymentAsync(int paymentId)
        {
            return await _context.ScheduledPayments
                .Include(s => s.Account)
                .Include(s => s.Beneficiary)
                .Include(s => s.DestinationAccount)
                .FirstOrDefaultAsync(s => s.Id == paymentId);
        }

        public async Task<bool> PauseScheduledPaymentAsync(int paymentId)
        {
            var payment = await _context.ScheduledPayments.FindAsync(paymentId);
            if (payment == null || payment.Status != ScheduledPaymentStatus.Active)
                return false;

            payment.Status = ScheduledPaymentStatus.Paused;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ResumeScheduledPaymentAsync(int paymentId)
        {
            var payment = await _context.ScheduledPayments.FindAsync(paymentId);
            if (payment == null || payment.Status != ScheduledPaymentStatus.Paused)
                return false;

            payment.Status = ScheduledPaymentStatus.Active;

            // Recalculate next execution date if it's in the past
            if (payment.NextExecutionDate < DateTime.UtcNow)
            {
                payment.NextExecutionDate = CalculateNextExecutionDate(payment.Frequency, DateTime.UtcNow);
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CancelScheduledPaymentAsync(int paymentId)
        {
            var payment = await _context.ScheduledPayments.FindAsync(paymentId);
            if (payment == null)
                return false;

            payment.Status = ScheduledPaymentStatus.Cancelled;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> ProcessDuePaymentsAsync()
        {
            var duePayments = await _context.ScheduledPayments
                .Include(s => s.Account)
                .Where(s => s.Status == ScheduledPaymentStatus.Active &&
                           s.NextExecutionDate <= DateTime.UtcNow)
                .ToListAsync();

            int processedCount = 0;

            foreach (var payment in duePayments)
            {
                try
                {
                    bool success = false;

                    if (payment.DestinationAccountId.HasValue)
                    {
                        success = await _accountService.TransferAsync(
                            payment.AccountId,
                            payment.DestinationAccountId.Value,
                            payment.Amount,
                            $"Scheduled: {payment.Name}");
                    }
                    else if (payment.BeneficiaryId.HasValue)
                    {
                        success = await _beneficiaryService.TransferToBeneficiaryAsync(
                            payment.AccountId,
                            payment.BeneficiaryId.Value,
                            payment.Amount,
                            $"Scheduled: {payment.Name}");
                    }

                    if (success)
                    {
                        payment.LastExecutedAt = DateTime.UtcNow;
                        payment.ExecutionCount++;

                        // Check if completed
                        if (payment.Frequency == PaymentFrequency.OneTime ||
                            (payment.MaxExecutions.HasValue && payment.ExecutionCount >= payment.MaxExecutions) ||
                            (payment.EndDate.HasValue && DateTime.UtcNow >= payment.EndDate))
                        {
                            payment.Status = ScheduledPaymentStatus.Completed;
                        }
                        else
                        {
                            payment.NextExecutionDate = CalculateNextExecutionDate(payment.Frequency, DateTime.UtcNow);
                        }

                        processedCount++;
                    }
                }
                catch
                {
                    // Log error but continue processing other payments
                }
            }

            await _context.SaveChangesAsync();

            return processedCount;
        }

        private DateTime CalculateNextExecutionDate(PaymentFrequency frequency, DateTime from)
        {
            return frequency switch
            {
                PaymentFrequency.Daily => from.AddDays(1),
                PaymentFrequency.Weekly => from.AddDays(7),
                PaymentFrequency.BiWeekly => from.AddDays(14),
                PaymentFrequency.Monthly => from.AddMonths(1),
                PaymentFrequency.Quarterly => from.AddMonths(3),
                PaymentFrequency.Yearly => from.AddYears(1),
                _ => from
            };
        }
    }
}
