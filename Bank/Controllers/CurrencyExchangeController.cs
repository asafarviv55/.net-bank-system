using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyExchangeController : ControllerBase
    {
        private readonly ICurrencyExchangeService _exchangeService;
        private readonly IAccountService _accountService;

        public CurrencyExchangeController(
            ICurrencyExchangeService exchangeService,
            IAccountService accountService)
        {
            _exchangeService = exchangeService;
            _accountService = accountService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet("rates")]
        public async Task<IActionResult> GetRates()
        {
            var rates = await _exchangeService.GetAllRatesAsync();
            return Ok(rates);
        }

        [HttpGet("rates/{baseCurrency}/{targetCurrency}")]
        public async Task<IActionResult> GetRate(string baseCurrency, string targetCurrency)
        {
            var rate = await _exchangeService.GetExchangeRateAsync(baseCurrency, targetCurrency);
            if (rate == null)
                return NotFound();

            return Ok(rate);
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] CalculateExchangeRequest request)
        {
            var amount = await _exchangeService.CalculateExchangeAmountAsync(
                request.FromCurrency,
                request.ToCurrency,
                request.Amount);

            return Ok(new { fromCurrency = request.FromCurrency, toCurrency = request.ToCurrency, fromAmount = request.Amount, toAmount = amount });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetExchangeHistory()
        {
            var userId = GetUserId();
            var exchanges = await _exchangeService.GetUserExchangesAsync(userId);
            return Ok(exchanges);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetExchange(int id)
        {
            var exchange = await _exchangeService.GetExchangeByIdAsync(id);
            if (exchange == null || exchange.UserId != GetUserId())
                return NotFound();

            return Ok(exchange);
        }

        [HttpPost("exchange")]
        public async Task<IActionResult> ExchangeCurrency([FromBody] ExchangeCurrencyRequest request)
        {
            var sourceAccount = await _accountService.GetAccountByIdAsync(request.SourceAccountId);
            if (sourceAccount == null || sourceAccount.UserId != GetUserId())
                return NotFound();

            if (request.DestinationAccountId.HasValue)
            {
                var destAccount = await _accountService.GetAccountByIdAsync(request.DestinationAccountId.Value);
                if (destAccount == null || destAccount.UserId != GetUserId())
                    return NotFound();
            }

            var userId = GetUserId();
            var exchange = await _exchangeService.ExchangeCurrencyAsync(
                userId,
                request.SourceAccountId,
                request.DestinationAccountId,
                request.FromCurrency,
                request.ToCurrency,
                request.Amount);

            if (exchange == null)
                return BadRequest(new { message = "Currency exchange failed" });

            return CreatedAtAction(nameof(GetExchange), new { id = exchange.Id }, exchange);
        }
    }

    public class CalculateExchangeRequest
    {
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal Amount { get; set; }
    }

    public class ExchangeCurrencyRequest
    {
        public int SourceAccountId { get; set; }
        public int? DestinationAccountId { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal Amount { get; set; }
    }
}
