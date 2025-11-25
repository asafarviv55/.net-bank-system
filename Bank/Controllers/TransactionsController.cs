using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IAccountService _accountService;

        public TransactionsController(
            ITransactionService transactionService,
            IAccountService accountService)
        {
            _transactionService = transactionService;
            _accountService = accountService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int accountId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string category = null)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var transactions = await _transactionService.GetTransactionsAsync(accountId, from, to, category);
            return Ok(transactions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransaction(int id)
        {
            var transaction = await _transactionService.GetTransactionByIdAsync(id);
            if (transaction == null)
                return NotFound();

            var account = await _accountService.GetAccountByIdAsync(transaction.AccountId);
            if (account.UserId != GetUserId())
                return Forbid();

            return Ok(transaction);
        }

        [HttpGet("reference/{referenceNumber}")]
        public async Task<IActionResult> GetTransactionByReference(string referenceNumber)
        {
            var transaction = await _transactionService.GetTransactionByReferenceAsync(referenceNumber);
            if (transaction == null)
                return NotFound();

            var account = await _accountService.GetAccountByIdAsync(transaction.AccountId);
            if (account.UserId != GetUserId())
                return Forbid();

            return Ok(transaction);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTransactions([FromQuery] int accountId, [FromQuery] string searchTerm)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var transactions = await _transactionService.SearchTransactionsAsync(accountId, searchTerm);
            return Ok(transactions);
        }
    }
}
