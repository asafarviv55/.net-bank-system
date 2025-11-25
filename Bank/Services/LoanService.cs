using Microsoft.EntityFrameworkCore;

namespace Bank.Services
{
    public interface ILoanService
    {
        Task<LoanApplication> CreateLoanApplicationAsync(int userId, LoanType type, decimal amount, int termMonths, string purpose);
        Task<LoanApplication> SubmitLoanApplicationAsync(int applicationId);
        Task<LoanApplication> GetLoanApplicationAsync(int applicationId);
        Task<IEnumerable<LoanApplication>> GetUserLoanApplicationsAsync(int userId);
        Task<(decimal monthlyPayment, decimal totalInterest, decimal totalPayment)> CalculateLoanAsync(decimal principal, decimal annualRate, int termMonths);
        Task<bool> ApproveLoanAsync(int applicationId, decimal approvedAmount, decimal interestRate);
        Task<bool> RejectLoanAsync(int applicationId, string reason);
        Task<bool> DisburseLoanAsync(int applicationId, int accountId);
    }

    public class LoanService : ILoanService
    {
        private readonly BankContext _context;
        private readonly IAccountService _accountService;

        public LoanService(BankContext context, IAccountService accountService)
        {
            _context = context;
            _accountService = accountService;
        }

        public async Task<LoanApplication> CreateLoanApplicationAsync(int userId, LoanType type, decimal amount, int termMonths, string purpose)
        {
            var application = new LoanApplication
            {
                ApplicationNumber = GenerateApplicationNumber(),
                LoanType = type,
                RequestedAmount = amount,
                TermMonths = termMonths,
                Purpose = purpose,
                UserId = userId,
                Status = LoanApplicationStatus.Draft
            };

            _context.LoanApplications.Add(application);
            await _context.SaveChangesAsync();

            return application;
        }

        public async Task<LoanApplication> SubmitLoanApplicationAsync(int applicationId)
        {
            var application = await _context.LoanApplications.FindAsync(applicationId);

            if (application == null || application.Status != LoanApplicationStatus.Draft)
                return null;

            application.Status = LoanApplicationStatus.Submitted;
            application.SubmittedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return application;
        }

        public async Task<LoanApplication> GetLoanApplicationAsync(int applicationId)
        {
            return await _context.LoanApplications
                .Include(l => l.User)
                .Include(l => l.Account)
                .FirstOrDefaultAsync(l => l.Id == applicationId);
        }

        public async Task<IEnumerable<LoanApplication>> GetUserLoanApplicationsAsync(int userId)
        {
            return await _context.LoanApplications
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public Task<(decimal monthlyPayment, decimal totalInterest, decimal totalPayment)> CalculateLoanAsync(decimal principal, decimal annualRate, int termMonths)
        {
            // EMI formula: P * r * (1+r)^n / ((1+r)^n - 1)
            var monthlyRate = annualRate / 100 / 12;

            decimal monthlyPayment;
            if (monthlyRate == 0)
            {
                monthlyPayment = principal / termMonths;
            }
            else
            {
                var factor = (decimal)Math.Pow((double)(1 + monthlyRate), termMonths);
                monthlyPayment = principal * monthlyRate * factor / (factor - 1);
            }

            var totalPayment = monthlyPayment * termMonths;
            var totalInterest = totalPayment - principal;

            return Task.FromResult((
                Math.Round(monthlyPayment, 2),
                Math.Round(totalInterest, 2),
                Math.Round(totalPayment, 2)
            ));
        }

        public async Task<bool> ApproveLoanAsync(int applicationId, decimal approvedAmount, decimal interestRate)
        {
            var application = await _context.LoanApplications.FindAsync(applicationId);

            if (application == null ||
                (application.Status != LoanApplicationStatus.Submitted &&
                 application.Status != LoanApplicationStatus.UnderReview))
                return false;

            var (monthlyPayment, _, _) = await CalculateLoanAsync(approvedAmount, interestRate, application.TermMonths);

            application.ApprovedAmount = approvedAmount;
            application.InterestRate = interestRate;
            application.MonthlyPayment = monthlyPayment;
            application.Status = LoanApplicationStatus.Approved;
            application.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RejectLoanAsync(int applicationId, string reason)
        {
            var application = await _context.LoanApplications.FindAsync(applicationId);

            if (application == null ||
                application.Status == LoanApplicationStatus.Disbursed ||
                application.Status == LoanApplicationStatus.Rejected)
                return false;

            application.Status = LoanApplicationStatus.Rejected;
            application.StatusReason = reason;
            application.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DisburseLoanAsync(int applicationId, int accountId)
        {
            var application = await _context.LoanApplications
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == applicationId);

            if (application == null || application.Status != LoanApplicationStatus.Approved)
                return false;

            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null || account.UserId != application.UserId)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create the loan record
                var loan = new Loan
                {
                    UserId = application.UserId.ToString(),
                    original_loan_fund = (int)application.ApprovedAmount.Value,
                    current_interest_rate = application.InterestRate.Value,
                    next_payment_date = DateTime.UtcNow.AddMonths(1),
                    next_payment_amount = application.MonthlyPayment.Value
                };

                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();

                // Deposit to account
                account.Balance += application.ApprovedAmount.Value;
                account.AvailableBalance += application.ApprovedAmount.Value;
                account.LastTransactionDate = DateTime.UtcNow;

                var txn = new Transaction
                {
                    ReferenceNumber = $"LOAN{application.ApplicationNumber}",
                    Type = TransactionType.Deposit,
                    Amount = application.ApprovedAmount.Value,
                    BalanceAfter = account.Balance,
                    Description = $"Loan disbursement: {application.LoanType}",
                    AccountId = accountId,
                    Status = TransactionStatus.Completed,
                    CompletedAt = DateTime.UtcNow,
                    Category = "Loan"
                };

                _context.Transactions.Add(txn);

                application.Status = LoanApplicationStatus.Disbursed;
                application.DisbursedAt = DateTime.UtcNow;
                application.AccountId = accountId;
                application.LoanId = loan.id;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        private string GenerateApplicationNumber()
        {
            return $"LA{DateTime.UtcNow:yyyyMMdd}{new Random().Next(10000, 99999)}";
        }
    }
}
