using Microsoft.EntityFrameworkCore;

namespace Bank.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(int userId, string title, string message, NotificationType type, NotificationPriority priority = NotificationPriority.Normal, string actionUrl = null, string actionText = null);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteNotificationAsync(int notificationId);
        Task NotifyTransactionAsync(int userId, Transaction transaction);
        Task NotifySecurityEventAsync(int userId, string eventDescription);
        Task NotifyLowBalanceAsync(int userId, Account account);
        Task NotifyBillPaymentAsync(int userId, BillPayment billPayment);
        Task<NotificationPreference> GetPreferencesAsync(int userId);
        Task<bool> UpdatePreferencesAsync(int userId, NotificationType notificationType, bool email, bool sms, bool push, bool inApp);
    }

    public class NotificationService : INotificationService
    {
        private readonly BankContext _context;

        public NotificationService(BankContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateNotificationAsync(
            int userId,
            string title,
            string message,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            string actionUrl = null,
            string actionText = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Priority = priority,
                Channel = NotificationChannel.InApp,
                ActionUrl = actionUrl,
                ActionText = actionText,
                SentAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Include(n => n.Transaction)
                .Include(n => n.Account)
                .Include(n => n.Card)
                .Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null || notification.IsRead)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task NotifyTransactionAsync(int userId, Transaction transaction)
        {
            var title = transaction.Type switch
            {
                TransactionType.Deposit => "Deposit Received",
                TransactionType.Withdrawal => "Withdrawal Processed",
                TransactionType.Transfer => "Transfer Completed",
                TransactionType.BillPayment => "Bill Payment Successful",
                _ => "Transaction Processed"
            };

            var message = $"Amount: ${Math.Abs(transaction.Amount):N2} - {transaction.Description}";

            await CreateNotificationAsync(
                userId,
                title,
                message,
                NotificationType.Transaction,
                NotificationPriority.Normal,
                $"/transactions/{transaction.Id}",
                "View Details"
            );
        }

        public async Task NotifySecurityEventAsync(int userId, string eventDescription)
        {
            await CreateNotificationAsync(
                userId,
                "Security Alert",
                eventDescription,
                NotificationType.Security,
                NotificationPriority.High,
                "/security/events",
                "Review"
            );
        }

        public async Task NotifyLowBalanceAsync(int userId, Account account)
        {
            await CreateNotificationAsync(
                userId,
                "Low Balance Alert",
                $"Your account {account.AccountNumber} has a low balance of ${account.Balance:N2}",
                NotificationType.Account,
                NotificationPriority.High,
                $"/accounts/{account.Id}",
                "View Account"
            );
        }

        public async Task NotifyBillPaymentAsync(int userId, BillPayment billPayment)
        {
            var status = billPayment.Status == BillPaymentStatus.Paid ? "successful" : "failed";
            await CreateNotificationAsync(
                userId,
                $"Bill Payment {status}",
                $"{billPayment.ProviderName} - ${billPayment.Amount:N2}",
                NotificationType.BillPayment,
                NotificationPriority.Normal,
                $"/bills/{billPayment.Id}",
                "View Details"
            );
        }

        public async Task<NotificationPreference> GetPreferencesAsync(int userId)
        {
            return await _context.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId);
        }

        public async Task<bool> UpdatePreferencesAsync(
            int userId,
            NotificationType notificationType,
            bool email,
            bool sms,
            bool push,
            bool inApp)
        {
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId && np.NotificationType == notificationType);

            if (preference == null)
            {
                preference = new NotificationPreference
                {
                    UserId = userId,
                    NotificationType = notificationType
                };
                _context.NotificationPreferences.Add(preference);
            }

            preference.EmailEnabled = email;
            preference.SMSEnabled = sms;
            preference.PushEnabled = push;
            preference.InAppEnabled = inApp;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
