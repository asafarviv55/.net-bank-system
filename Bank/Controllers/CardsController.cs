using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CardsController : ControllerBase
    {
        private readonly ICardService _cardService;
        private readonly IAccountService _accountService;

        public CardsController(ICardService cardService, IAccountService accountService)
        {
            _cardService = cardService;
            _accountService = accountService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> GetCards()
        {
            var userId = GetUserId();
            var cards = await _cardService.GetUserCardsAsync(userId);
            return Ok(cards);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCard(int id)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            return Ok(card);
        }

        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetAccountCards(int accountId)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var cards = await _cardService.GetAccountCardsAsync(accountId);
            return Ok(cards);
        }

        [HttpPost]
        public async Task<IActionResult> IssueCard([FromBody] IssueCardRequest request)
        {
            var account = await _accountService.GetAccountByIdAsync(request.AccountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var card = await _cardService.IssueCardAsync(request.AccountId, request.Type, request.CardholderName);
            if (card == null)
                return BadRequest(new { message = "Failed to issue card" });

            return CreatedAtAction(nameof(GetCard), new { id = card.Id }, card);
        }

        [HttpPost("{id}/block")]
        public async Task<IActionResult> BlockCard(int id, [FromBody] BlockCardRequest request)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            var success = await _cardService.BlockCardAsync(id, request.Reason);
            if (!success)
                return BadRequest(new { message = "Failed to block card" });

            return Ok(new { message = "Card blocked successfully" });
        }

        [HttpPost("{id}/unblock")]
        public async Task<IActionResult> UnblockCard(int id)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            var success = await _cardService.UnblockCardAsync(id);
            if (!success)
                return BadRequest(new { message = "Failed to unblock card" });

            return Ok(new { message = "Card unblocked successfully" });
        }

        [HttpPost("{id}/freeze")]
        public async Task<IActionResult> FreezeCard(int id)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            var success = await _cardService.FreezeCardAsync(id);
            if (!success)
                return BadRequest(new { message = "Failed to freeze card" });

            return Ok(new { message = "Card frozen successfully" });
        }

        [HttpPost("{id}/unfreeze")]
        public async Task<IActionResult> UnfreezeCard(int id)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            var success = await _cardService.UnfreezeCardAsync(id);
            if (!success)
                return BadRequest(new { message = "Failed to unfreeze card" });

            return Ok(new { message = "Card unfrozen successfully" });
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelCard(int id)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            var success = await _cardService.CancelCardAsync(id);
            if (!success)
                return BadRequest(new { message = "Failed to cancel card" });

            return Ok(new { message = "Card cancelled successfully" });
        }

        [HttpPut("{id}/limits")]
        public async Task<IActionResult> UpdateLimits(int id, [FromBody] UpdateLimitsRequest request)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            var success = await _cardService.UpdateLimitsAsync(
                id,
                request.DailyWithdrawalLimit,
                request.DailyTransactionLimit,
                request.OnlineTransactionLimit);

            if (!success)
                return BadRequest(new { message = "Failed to update limits" });

            return Ok(new { message = "Limits updated successfully" });
        }

        [HttpPost("{id}/pin")]
        public async Task<IActionResult> SetPIN(int id, [FromBody] SetPINRequest request)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            var success = await _cardService.SetPINAsync(id, request.PIN);
            if (!success)
                return BadRequest(new { message = "Failed to set PIN" });

            return Ok(new { message = "PIN set successfully" });
        }

        [HttpPost("{id}/online-payments")]
        public async Task<IActionResult> ToggleOnlinePayments(int id, [FromBody] ToggleRequest request)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            var success = await _cardService.ToggleOnlinePaymentsAsync(id, request.Enabled);
            if (!success)
                return BadRequest(new { message = "Failed to toggle online payments" });

            return Ok(new { message = $"Online payments {(request.Enabled ? "enabled" : "disabled")}" });
        }

        [HttpPost("{id}/international-payments")]
        public async Task<IActionResult> ToggleInternationalPayments(int id, [FromBody] ToggleRequest request)
        {
            var card = await _cardService.GetCardByIdAsync(id);
            if (card == null || card.Account.UserId != GetUserId())
                return NotFound();

            var success = await _cardService.ToggleInternationalPaymentsAsync(id, request.Enabled);
            if (!success)
                return BadRequest(new { message = "Failed to toggle international payments" });

            return Ok(new { message = $"International payments {(request.Enabled ? "enabled" : "disabled")}" });
        }
    }

    public class IssueCardRequest
    {
        public int AccountId { get; set; }
        public CardType Type { get; set; }
        public string CardholderName { get; set; }
    }

    public class BlockCardRequest
    {
        public string Reason { get; set; }
    }

    public class UpdateLimitsRequest
    {
        public decimal? DailyWithdrawalLimit { get; set; }
        public decimal? DailyTransactionLimit { get; set; }
        public decimal? OnlineTransactionLimit { get; set; }
    }

    public class SetPINRequest
    {
        public string PIN { get; set; }
    }

    public class ToggleRequest
    {
        public bool Enabled { get; set; }
    }
}
