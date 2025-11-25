using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BeneficiariesController : ControllerBase
    {
        private readonly IBeneficiaryService _beneficiaryService;
        private readonly IAccountService _accountService;

        public BeneficiariesController(
            IBeneficiaryService beneficiaryService,
            IAccountService accountService)
        {
            _beneficiaryService = beneficiaryService;
            _accountService = accountService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> GetBeneficiaries([FromQuery] int accountId)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var beneficiaries = await _beneficiaryService.GetBeneficiariesAsync(accountId);
            return Ok(beneficiaries);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBeneficiary(int id)
        {
            var beneficiary = await _beneficiaryService.GetBeneficiaryAsync(id);
            if (beneficiary == null)
                return NotFound();

            var account = await _accountService.GetAccountByIdAsync(beneficiary.AccountId);
            if (account.UserId != GetUserId())
                return Forbid();

            return Ok(beneficiary);
        }

        [HttpPost]
        public async Task<IActionResult> AddBeneficiary([FromBody] AddBeneficiaryRequest request)
        {
            var account = await _accountService.GetAccountByIdAsync(request.AccountId);
            if (account == null || account.UserId != GetUserId())
                return NotFound();

            var beneficiary = await _beneficiaryService.AddBeneficiaryAsync(
                request.AccountId,
                request.BeneficiaryName,
                request.Nickname,
                request.BeneficiaryType,
                request.BeneficiaryAccountNumber,
                request.BeneficiaryBankName);

            if (beneficiary == null)
                return BadRequest(new { message = "Failed to add beneficiary" });

            return CreatedAtAction(nameof(GetBeneficiary), new { id = beneficiary.Id }, beneficiary);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBeneficiary(int id, [FromBody] UpdateBeneficiaryRequest request)
        {
            var beneficiary = await _beneficiaryService.GetBeneficiaryAsync(id);
            if (beneficiary == null)
                return NotFound();

            var account = await _accountService.GetAccountByIdAsync(beneficiary.AccountId);
            if (account.UserId != GetUserId())
                return Forbid();

            var success = await _beneficiaryService.UpdateBeneficiaryAsync(id, request.Nickname);
            if (!success)
                return BadRequest(new { message = "Failed to update beneficiary" });

            return Ok(new { message = "Beneficiary updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBeneficiary(int id)
        {
            var beneficiary = await _beneficiaryService.GetBeneficiaryAsync(id);
            if (beneficiary == null)
                return NotFound();

            var account = await _accountService.GetAccountByIdAsync(beneficiary.AccountId);
            if (account.UserId != GetUserId())
                return Forbid();

            var success = await _beneficiaryService.DeleteBeneficiaryAsync(id);
            if (!success)
                return BadRequest(new { message = "Failed to delete beneficiary" });

            return Ok(new { message = "Beneficiary deleted successfully" });
        }
    }

    public class AddBeneficiaryRequest
    {
        public int AccountId { get; set; }
        public string BeneficiaryName { get; set; }
        public string Nickname { get; set; }
        public BeneficiaryType BeneficiaryType { get; set; }
        public string BeneficiaryAccountNumber { get; set; }
        public string BeneficiaryBankName { get; set; }
    }

    public class UpdateBeneficiaryRequest
    {
        public string Nickname { get; set; }
    }
}
