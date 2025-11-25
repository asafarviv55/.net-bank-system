using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var userId = GetUserId();
            var logs = await _auditService.GetUserAuditLogsAsync(userId, startDate, endDate);
            return Ok(logs);
        }

        [HttpGet("logs/account/{accountId}")]
        public async Task<IActionResult> GetAccountAuditLogs(
            int accountId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var logs = await _auditService.GetAccountAuditLogsAsync(accountId, startDate, endDate);
            return Ok(logs);
        }

        [HttpGet("logs/action/{action}")]
        public async Task<IActionResult> GetAuditLogsByAction(
            string action,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            if (!Enum.TryParse<AuditAction>(action, true, out var auditAction))
                return BadRequest(new { message = "Invalid action" });

            var logs = await _auditService.GetAuditLogsByActionAsync(auditAction, startDate, endDate);
            return Ok(logs);
        }

        [HttpGet("logs/critical")]
        public async Task<IActionResult> GetCriticalAuditLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var logs = await _auditService.GetCriticalAuditLogsAsync(startDate, endDate);
            return Ok(logs);
        }

        [HttpGet("security-events")]
        public async Task<IActionResult> GetSecurityEvents([FromQuery] bool unresolvedOnly = false)
        {
            var userId = GetUserId();
            var events = await _auditService.GetUserSecurityEventsAsync(userId, unresolvedOnly);
            return Ok(events);
        }

        [HttpPost("security-events/{id}/resolve")]
        public async Task<IActionResult> ResolveSecurityEvent(int id, [FromBody] ResolveEventRequest request)
        {
            var success = await _auditService.ResolveSecurityEventAsync(id, request.ResolutionNotes);
            if (!success)
                return NotFound();

            return Ok(new { message = "Security event resolved" });
        }
    }

    public class ResolveEventRequest
    {
        public string ResolutionNotes { get; set; }
    }
}
