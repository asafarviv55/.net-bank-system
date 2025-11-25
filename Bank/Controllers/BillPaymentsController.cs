using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BillPaymentsController : ControllerBase
    {
        private readonly IBillPaymentService _billPaymentService;
        private readonly IAccountService _accountService;

        public BillPaymentsController(
            IBillPaymentService billPaymentService,
            IAccountService accountService)
        {
            _billPaymentService = billPaymentService;
            _accountService = accountService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> GetBillPayments([FromQuery] int accountId, [FromQuery] BillPaymentStatus? status = null)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var billPayments = await _billPaymentService.GetBillPaymentsAsync(accountId, status);
            return Ok(billPayments);
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingBills([FromQuery] int accountId, [FromQuery] int daysAhead = 30)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var billPayments = await _billPaymentService.GetUpcomingBillsAsync(accountId, daysAhead);
            return Ok(billPayments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBillPayment([FromBody] CreateBillPaymentRequest request)
        {
            var account = await _accountService.GetAccountByIdAsync(request.AccountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var billPayment = await _billPaymentService.CreateBillPaymentAsync(
                request.AccountId,
                request.ProviderName,
                request.CustomerAccountNumber,
                request.Category,
                request.Amount,
                request.DueDate);

            if (billPayment == null)
                return BadRequest(new { message = "Failed to create bill payment" });

            return Ok(billPayment);
        }

        [HttpPost("{id}/pay")]
        public async Task<IActionResult> PayBill(int id)
        {
            var success = await _billPaymentService.PayBillAsync(id);
            if (!success)
                return BadRequest(new { message = "Insufficient funds or payment failed" });

            return Ok(new { message = "Bill payment processed successfully" });
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBillPayment(int id)
        {
            var success = await _billPaymentService.CancelBillPaymentAsync(id);
            if (!success)
                return BadRequest(new { message = "Failed to cancel bill payment" });

            return Ok(new { message = "Bill payment cancelled successfully" });
        }
    }

    public class CreateBillPaymentRequest
    {
        public int AccountId { get; set; }
        public string ProviderName { get; set; }
        public string CustomerAccountNumber { get; set; }
        public BillCategory Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string Notes { get; set; }
    }
}
