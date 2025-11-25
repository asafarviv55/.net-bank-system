using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Bank.Services
{
    public interface ICardService
    {
        Task<Card> IssueCardAsync(int accountId, CardType type, string cardholderName);
        Task<Card> GetCardByIdAsync(int cardId);
        Task<Card> GetCardByNumberAsync(string cardNumber);
        Task<IEnumerable<Card>> GetAccountCardsAsync(int accountId);
        Task<IEnumerable<Card>> GetUserCardsAsync(int userId);
        Task<bool> BlockCardAsync(int cardId, string reason);
        Task<bool> UnblockCardAsync(int cardId);
        Task<bool> FreezeCardAsync(int cardId);
        Task<bool> UnfreezeCardAsync(int cardId);
        Task<bool> CancelCardAsync(int cardId);
        Task<bool> UpdateLimitsAsync(int cardId, decimal? dailyWithdrawal, decimal? dailyTransaction, decimal? onlineTransaction);
        Task<bool> SetPINAsync(int cardId, string pin);
        Task<bool> ValidatePINAsync(int cardId, string pin);
        Task<bool> ToggleOnlinePaymentsAsync(int cardId, bool enabled);
        Task<bool> ToggleInternationalPaymentsAsync(int cardId, bool enabled);
    }

    public class CardService : ICardService
    {
        private readonly BankContext _context;
        private readonly IAuditService _auditService;

        public CardService(BankContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<Card> IssueCardAsync(int accountId, CardType type, string cardholderName)
        {
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null || !account.IsActive)
                return null;

            var cardNumber = GenerateCardNumber();
            var cvv = GenerateCVV();
            var expiryDate = DateTime.UtcNow.AddYears(3);

            var card = new Card
            {
                CardNumber = cardNumber,
                CardholderName = cardholderName,
                Type = type,
                CVV = cvv,
                ExpiryDate = expiryDate,
                AccountId = accountId,
                Status = CardStatus.Active,
                DailyWithdrawalLimit = 5000,
                DailyTransactionLimit = 10000,
                OnlineTransactionLimit = 5000,
                OnlinePaymentsEnabled = true,
                InternationalPaymentsEnabled = false,
                ContactlessEnabled = true
            };

            if (type == CardType.Credit)
            {
                card.CreditLimit = 10000; // Default credit limit
                card.AvailableCredit = card.CreditLimit;
            }

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                AuditAction.CardIssued,
                $"Card {cardNumber.Substring(cardNumber.Length - 4)} issued for account {account.AccountNumber}",
                account.UserId,
                accountId: accountId
            );

            return card;
        }

        public async Task<Card> GetCardByIdAsync(int cardId)
        {
            return await _context.Cards
                .Include(c => c.Account)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(c => c.Id == cardId);
        }

        public async Task<Card> GetCardByNumberAsync(string cardNumber)
        {
            return await _context.Cards
                .Include(c => c.Account)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
        }

        public async Task<IEnumerable<Card>> GetAccountCardsAsync(int accountId)
        {
            return await _context.Cards
                .Where(c => c.AccountId == accountId)
                .OrderByDescending(c => c.IssuedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Card>> GetUserCardsAsync(int userId)
        {
            return await _context.Cards
                .Include(c => c.Account)
                .Where(c => c.Account.UserId == userId)
                .OrderByDescending(c => c.IssuedDate)
                .ToListAsync();
        }

        public async Task<bool> BlockCardAsync(int cardId, string reason)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null) return false;

            card.Status = CardStatus.Blocked;
            card.BlockedAt = DateTime.UtcNow;
            card.BlockReason = reason;

            await _context.SaveChangesAsync();

            var account = await _context.Accounts.FindAsync(card.AccountId);
            await _auditService.LogAsync(
                AuditAction.CardBlocked,
                $"Card ending in {card.CardNumber.Substring(card.CardNumber.Length - 4)} blocked: {reason}",
                account.UserId,
                accountId: card.AccountId
            );

            return true;
        }

        public async Task<bool> UnblockCardAsync(int cardId)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null || card.Status != CardStatus.Blocked) return false;

            card.Status = CardStatus.Active;
            card.BlockedAt = null;
            card.BlockReason = null;

            await _context.SaveChangesAsync();

            var account = await _context.Accounts.FindAsync(card.AccountId);
            await _auditService.LogAsync(
                AuditAction.CardUnblocked,
                $"Card ending in {card.CardNumber.Substring(card.CardNumber.Length - 4)} unblocked",
                account.UserId,
                accountId: card.AccountId
            );

            return true;
        }

        public async Task<bool> FreezeCardAsync(int cardId)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null || card.Status != CardStatus.Active) return false;

            card.Status = CardStatus.Frozen;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnfreezeCardAsync(int cardId)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null || card.Status != CardStatus.Frozen) return false;

            card.Status = CardStatus.Active;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CancelCardAsync(int cardId)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null) return false;

            card.Status = CardStatus.Cancelled;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateLimitsAsync(int cardId, decimal? dailyWithdrawal, decimal? dailyTransaction, decimal? onlineTransaction)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null) return false;

            if (dailyWithdrawal.HasValue)
                card.DailyWithdrawalLimit = dailyWithdrawal.Value;

            if (dailyTransaction.HasValue)
                card.DailyTransactionLimit = dailyTransaction.Value;

            if (onlineTransaction.HasValue)
                card.OnlineTransactionLimit = onlineTransaction.Value;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetPINAsync(int cardId, string pin)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null) return false;

            card.PINHash = HashPIN(pin);
            card.PINAttempts = 0;
            card.LastPINAttempt = null;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ValidatePINAsync(int cardId, string pin)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null) return false;

            card.LastPINAttempt = DateTime.UtcNow;

            if (card.PINAttempts >= 3)
            {
                card.Status = CardStatus.Blocked;
                card.BlockReason = "Too many incorrect PIN attempts";
                await _context.SaveChangesAsync();
                return false;
            }

            var isValid = card.PINHash == HashPIN(pin);

            if (isValid)
            {
                card.PINAttempts = 0;
            }
            else
            {
                card.PINAttempts++;
            }

            await _context.SaveChangesAsync();

            return isValid;
        }

        public async Task<bool> ToggleOnlinePaymentsAsync(int cardId, bool enabled)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null) return false;

            card.OnlinePaymentsEnabled = enabled;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleInternationalPaymentsAsync(int cardId, bool enabled)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card == null) return false;

            card.InternationalPaymentsEnabled = enabled;
            await _context.SaveChangesAsync();

            return true;
        }

        private string GenerateCardNumber()
        {
            // Generate a 16-digit card number (simplified)
            var random = new Random();
            var cardNumber = "4532"; // Start with BIN (Bank Identification Number)
            for (int i = 0; i < 12; i++)
            {
                cardNumber += random.Next(0, 10).ToString();
            }
            return cardNumber;
        }

        private string GenerateCVV()
        {
            var random = new Random();
            return random.Next(100, 1000).ToString();
        }

        private string HashPIN(string pin)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pin));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
