using Microsoft.EntityFrameworkCore;

namespace Bank.Services
{
    public interface IBeneficiaryService
    {
        Task<Beneficiary> AddBeneficiaryAsync(int accountId, string name, string nickname, BeneficiaryType type, string accountNumber, string bankName = null);
        Task<Beneficiary> GetBeneficiaryAsync(int beneficiaryId);
        Task<IEnumerable<Beneficiary>> GetBeneficiariesAsync(int accountId);
        Task<bool> UpdateBeneficiaryAsync(int beneficiaryId, string nickname);
        Task<bool> DeleteBeneficiaryAsync(int beneficiaryId);
        Task<bool> TransferToBeneficiaryAsync(int accountId, int beneficiaryId, decimal amount, string description);
    }

    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly BankContext _context;

        public BeneficiaryService(BankContext context)
        {
            _context = context;
        }

        public async Task<Beneficiary> AddBeneficiaryAsync(int accountId, string name, string nickname, BeneficiaryType type, string accountNumber, string bankName = null)
        {
            var beneficiary = new Beneficiary
            {
                Name = name,
                Nickname = nickname ?? name,
                Type = type,
                AccountNumber = accountNumber,
                BankName = bankName,
                AccountId = accountId,
                IsActive = true
            };

            _context.Beneficiaries.Add(beneficiary);
            await _context.SaveChangesAsync();

            return beneficiary;
        }

        public async Task<Beneficiary> GetBeneficiaryAsync(int beneficiaryId)
        {
            return await _context.Beneficiaries
                .Include(b => b.Account)
                .FirstOrDefaultAsync(b => b.Id == beneficiaryId);
        }

        public async Task<IEnumerable<Beneficiary>> GetBeneficiariesAsync(int accountId)
        {
            return await _context.Beneficiaries
                .Where(b => b.AccountId == accountId && b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<bool> UpdateBeneficiaryAsync(int beneficiaryId, string nickname)
        {
            var beneficiary = await _context.Beneficiaries.FindAsync(beneficiaryId);
            if (beneficiary == null) return false;

            beneficiary.Nickname = nickname;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteBeneficiaryAsync(int beneficiaryId)
        {
            var beneficiary = await _context.Beneficiaries.FindAsync(beneficiaryId);
            if (beneficiary == null) return false;

            beneficiary.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> TransferToBeneficiaryAsync(int accountId, int beneficiaryId, decimal amount, string description)
        {
            if (amount <= 0) return false;

            var account = await _context.Accounts.FindAsync(accountId);
            var beneficiary = await _context.Beneficiaries.FindAsync(beneficiaryId);

            if (account == null || beneficiary == null) return false;
            if (!account.IsActive || !beneficiary.IsActive) return false;
            if (beneficiary.AccountId != accountId) return false;
            if (account.AvailableBalance < amount) return false;

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                account.Balance -= amount;
                account.AvailableBalance -= amount;
                account.LastTransactionDate = DateTime.UtcNow;

                var transaction = new Transaction
                {
                    ReferenceNumber = GenerateReferenceNumber(),
                    Type = TransactionType.Transfer,
                    Amount = -amount,
                    BalanceAfter = account.Balance,
                    Description = description ?? $"Transfer to {beneficiary.Name}",
                    AccountId = accountId,
                    BeneficiaryId = beneficiaryId,
                    Status = TransactionStatus.Completed,
                    CompletedAt = DateTime.UtcNow,
                    Category = "Transfer"
                };

                beneficiary.LastUsedAt = DateTime.UtcNow;

                _context.Transactions.Add(transaction);
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

        private string GenerateReferenceNumber()
        {
            return $"BEN{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}
