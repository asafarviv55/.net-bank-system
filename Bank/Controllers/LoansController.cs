using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoansController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet("applications")]
        public async Task<IActionResult> GetApplications()
        {
            var userId = GetUserId();
            var applications = await _loanService.GetUserLoanApplicationsAsync(userId);
            return Ok(applications);
        }

        [HttpGet("applications/{id}")]
        public async Task<IActionResult> GetApplication(int id)
        {
            var application = await _loanService.GetLoanApplicationAsync(id);
            if (application == null || application.UserId != GetUserId())
                return NotFound();

            return Ok(application);
        }

        [HttpPost("applications")]
        public async Task<IActionResult> CreateApplication([FromBody] CreateLoanApplicationRequest request)
        {
            var userId = GetUserId();
            var application = await _loanService.CreateLoanApplicationAsync(
                userId,
                request.LoanType,
                request.RequestedAmount,
                request.TermMonths,
                request.Purpose);

            if (application == null)
                return BadRequest(new { message = "Failed to create loan application" });

            return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, application);
        }

        [HttpPost("applications/{id}/submit")]
        public async Task<IActionResult> SubmitApplication(int id)
        {
            var application = await _loanService.GetLoanApplicationAsync(id);
            if (application == null || application.UserId != GetUserId())
                return NotFound();

            var result = await _loanService.SubmitLoanApplicationAsync(id);
            if (result == null)
                return BadRequest(new { message = "Failed to submit application" });

            return Ok(new { message = "Application submitted successfully" });
        }

        [HttpGet("calculator")]
        public async Task<IActionResult> CalculateLoan(
            [FromQuery] decimal amount,
            [FromQuery] int termMonths,
            [FromQuery] decimal interestRate)
        {
            var calculation = await _loanService.CalculateLoanAsync(amount, interestRate, termMonths);
            return Ok(new
            {
                monthlyPayment = calculation.monthlyPayment,
                totalInterest = calculation.totalInterest,
                totalPayment = calculation.totalPayment
            });
        }
    }

    public class CreateLoanApplicationRequest
    {
        public LoanType LoanType { get; set; }
        public decimal RequestedAmount { get; set; }
        public int TermMonths { get; set; }
        public string Purpose { get; set; }
        public string EmployerName { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
    }
}
