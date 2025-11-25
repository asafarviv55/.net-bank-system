using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> GetAccounts()
        {
            var userId = GetUserId();
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            return Ok(accounts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccount(int id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            return Ok(account);
        }

        [HttpGet("number/{accountNumber}")]
        public async Task<IActionResult> GetAccountByNumber(string accountNumber)
        {
            var account = await _accountService.GetAccountByNumberAsync(accountNumber);
            if (account == null)
                return NotFound();

            return Ok(account);
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetTotalBalance()
        {
            var userId = GetUserId();
            var balance = await _accountService.GetTotalBalanceAsync(userId);
            return Ok(new { totalBalance = balance });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            var userId = GetUserId();
            var account = await _accountService.CreateAccountAsync(userId, request.Type, request.Name);
            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
        }

        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> Deposit(int id, [FromBody] TransactionRequest request)
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var success = await _accountService.DepositAsync(id, request.Amount, request.Description);
            if (!success)
                return BadRequest(new { message = "Deposit failed" });

            return Ok(new { message = "Deposit successful" });
        }

        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> Withdraw(int id, [FromBody] TransactionRequest request)
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var success = await _accountService.WithdrawAsync(id, request.Amount, request.Description);
            if (!success)
                return BadRequest(new { message = "Insufficient funds or invalid request" });

            return Ok(new { message = "Withdrawal successful" });
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            var fromAccount = await _accountService.GetAccountByIdAsync(request.FromAccountId);
            if (fromAccount == null || fromAccount.UserId != GetUserId())
                return NotFound();

            var success = await _accountService.TransferAsync(
                request.FromAccountId,
                request.ToAccountId,
                request.Amount,
                request.Description);

            if (!success)
                return BadRequest(new { message = "Transfer failed" });

            return Ok(new { message = "Transfer successful" });
        }
    }

    public class CreateAccountRequest
    {
        public AccountType Type { get; set; }
        public string Name { get; set; }
    }

    public class TransactionRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }

    public class TransferRequest
    {
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}
