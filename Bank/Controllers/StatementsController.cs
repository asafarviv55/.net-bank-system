using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StatementsController : ControllerBase
    {
        private readonly IStatementService _statementService;
        private readonly IAccountService _accountService;

        public StatementsController(
            IStatementService statementService,
            IAccountService accountService)
        {
            _statementService = statementService;
            _accountService = accountService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> GetStatements([FromQuery] int accountId)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var statements = await _statementService.GetStatementsAsync(accountId);
            return Ok(statements);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStatement(int id)
        {
            var statement = await _statementService.GetStatementAsync(id);
            if (statement == null)
                return NotFound();

            var account = await _accountService.GetAccountByIdAsync(statement.AccountId);
            if (account.UserId != GetUserId())
                return Forbid();

            return Ok(statement);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateStatement([FromBody] GenerateStatementRequest request)
        {
            var account = await _accountService.GetAccountByIdAsync(request.AccountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var statement = await _statementService.GenerateStatementAsync(
                request.AccountId,
                request.StartDate,
                request.EndDate,
                request.Format);

            if (statement == null)
                return BadRequest(new { message = "Failed to generate statement" });

            return Ok(statement);
        }

        [HttpGet("download/csv")]
        public async Task<IActionResult> DownloadCSV(
            [FromQuery] int accountId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var csvContent = await _statementService.GenerateCSVContentAsync(accountId, startDate, endDate);
            if (csvContent == null)
                return NotFound();

            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", $"Statement_{accountId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
        }

        [HttpGet("download/pdf")]
        public async Task<IActionResult> DownloadPDF(
            [FromQuery] int accountId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var pdfBytes = await _statementService.GeneratePDFContentAsync(accountId, startDate, endDate);
            if (pdfBytes == null)
                return NotFound();

            return File(pdfBytes, "application/pdf", $"Statement_{accountId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
        }
    }

    public class GenerateStatementRequest
    {
        public int AccountId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public StatementFormat Format { get; set; } = StatementFormat.PDF;
    }
}
