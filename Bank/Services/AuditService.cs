using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Bank.Services
{
    public interface IAuditService
    {
        Task LogAsync(AuditAction action, string description, int? userId = null, string entityType = null, string entityId = null, int? accountId = null, int? transactionId = null, object oldValues = null, object newValues = null, AuditSeverity severity = AuditSeverity.Info);
        Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<AuditLog>> GetAccountAuditLogsAsync(int accountId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(AuditAction action, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<AuditLog>> GetCriticalAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<SecurityEvent> LogSecurityEventAsync(int? userId, string eventType, string description, AuditSeverity severity, string ipAddress = null, string userAgent = null, string location = null);
        Task<IEnumerable<SecurityEvent>> GetUserSecurityEventsAsync(int userId, bool unresolvedOnly = false);
        Task<bool> ResolveSecurityEventAsync(int eventId, string resolutionNotes);
    }

    public class AuditService : IAuditService
    {
        private readonly BankContext _context;

        public AuditService(BankContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            AuditAction action,
            string description,
            int? userId = null,
            string entityType = null,
            string entityId = null,
            int? accountId = null,
            int? transactionId = null,
            object oldValues = null,
            object newValues = null,
            AuditSeverity severity = AuditSeverity.Info)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                Description = description,
                Severity = severity,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                AccountId = accountId,
                TransactionId = transactionId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.AuditLogs
                .Include(al => al.User)
                .Include(al => al.Account)
                .Include(al => al.Transaction)
                .Where(al => al.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(al => al.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(al => al.Timestamp)
                .Take(100)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAccountAuditLogsAsync(
            int accountId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.AuditLogs
                .Include(al => al.User)
                .Include(al => al.Account)
                .Include(al => al.Transaction)
                .Where(al => al.AccountId == accountId);

            if (startDate.HasValue)
                query = query.Where(al => al.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(al => al.Timestamp)
                .Take(100)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(
            AuditAction action,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.AuditLogs
                .Include(al => al.User)
                .Include(al => al.Account)
                .Include(al => al.Transaction)
                .Where(al => al.Action == action);

            if (startDate.HasValue)
                query = query.Where(al => al.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(al => al.Timestamp)
                .Take(100)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetCriticalAuditLogsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.AuditLogs
                .Include(al => al.User)
                .Include(al => al.Account)
                .Include(al => al.Transaction)
                .Where(al => al.Severity == AuditSeverity.Critical || al.Severity == AuditSeverity.Error);

            if (startDate.HasValue)
                query = query.Where(al => al.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(al => al.Timestamp)
                .Take(100)
                .ToListAsync();
        }

        public async Task<SecurityEvent> LogSecurityEventAsync(
            int? userId,
            string eventType,
            string description,
            AuditSeverity severity,
            string ipAddress = null,
            string userAgent = null,
            string location = null)
        {
            var securityEvent = new SecurityEvent
            {
                UserId = userId,
                EventType = eventType,
                Description = description,
                Severity = severity,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Location = location,
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();

            // Also log to audit
            await LogAsync(
                AuditAction.SecurityAlert,
                description,
                userId,
                "SecurityEvent",
                securityEvent.Id.ToString(),
                severity: severity
            );

            return securityEvent;
        }

        public async Task<IEnumerable<SecurityEvent>> GetUserSecurityEventsAsync(int userId, bool unresolvedOnly = false)
        {
            var query = _context.SecurityEvents
                .Where(se => se.UserId == userId);

            if (unresolvedOnly)
                query = query.Where(se => !se.IsResolved);

            return await query
                .OrderByDescending(se => se.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<bool> ResolveSecurityEventAsync(int eventId, string resolutionNotes)
        {
            var securityEvent = await _context.SecurityEvents.FindAsync(eventId);
            if (securityEvent == null || securityEvent.IsResolved)
                return false;

            securityEvent.IsResolved = true;
            securityEvent.ResolvedAt = DateTime.UtcNow;
            securityEvent.ResolutionNotes = resolutionNotes;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
