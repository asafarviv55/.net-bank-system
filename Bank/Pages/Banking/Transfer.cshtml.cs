using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Bank.Pages.Banking
{
    [Authorize]
    public class TransferModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly IBeneficiaryService _beneficiaryService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransferModel(
            IAccountService accountService,
            IBeneficiaryService beneficiaryService,
            UserManager<ApplicationUser> userManager)
        {
            _accountService = accountService;
            _beneficiaryService = beneficiaryService;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public SelectList FromAccounts { get; set; }
        public SelectList ToAccounts { get; set; }
        public SelectList Beneficiaries { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "From Account")]
            public int FromAccountId { get; set; }

            [Display(Name = "To Account")]
            public int? ToAccountId { get; set; }

            [Display(Name = "Beneficiary")]
            public int? BeneficiaryId { get; set; }

            [Required]
            [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
            public decimal Amount { get; set; }

            [MaxLength(500)]
            public string Description { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? fromId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await LoadSelectListsAsync(user.Id);

            if (fromId.HasValue)
            {
                Input = new InputModel { FromAccountId = fromId.Value };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync(user.Id);
                return Page();
            }

            // Validate source account belongs to user
            var fromAccount = await _accountService.GetAccountByIdAsync(Input.FromAccountId);
            if (fromAccount == null || fromAccount.UserId != user.Id)
            {
                ModelState.AddModelError("", "Invalid source account");
                await LoadSelectListsAsync(user.Id);
                return Page();
            }

            bool success = false;

            if (Input.ToAccountId.HasValue)
            {
                // Internal transfer
                success = await _accountService.TransferAsync(
                    Input.FromAccountId,
                    Input.ToAccountId.Value,
                    Input.Amount,
                    Input.Description);
            }
            else if (Input.BeneficiaryId.HasValue)
            {
                // Transfer to beneficiary
                success = await _beneficiaryService.TransferToBeneficiaryAsync(
                    Input.FromAccountId,
                    Input.BeneficiaryId.Value,
                    Input.Amount,
                    Input.Description);
            }
            else
            {
                ModelState.AddModelError("", "Please select a destination account or beneficiary");
                await LoadSelectListsAsync(user.Id);
                return Page();
            }

            if (!success)
            {
                ModelState.AddModelError("", "Transfer failed. Please check your balance and try again.");
                await LoadSelectListsAsync(user.Id);
                return Page();
            }

            TempData["SuccessMessage"] = $"Successfully transferred {Input.Amount:C}";
            return RedirectToPage("Dashboard");
        }

        private async Task LoadSelectListsAsync(int userId)
        {
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            var accountList = accounts.Select(a => new { a.Id, Display = $"{a.AccountName} ({a.AccountNumber}) - {a.Balance:C}" }).ToList();

            FromAccounts = new SelectList(accountList, "Id", "Display");
            ToAccounts = new SelectList(accountList, "Id", "Display");

            var beneficiaryList = new List<object>();
            foreach (var account in accounts)
            {
                var beneficiaries = await _beneficiaryService.GetBeneficiariesAsync(account.Id);
                beneficiaryList.AddRange(beneficiaries.Select(b => new { b.Id, Display = $"{b.Nickname} - {b.AccountNumber}" }));
            }

            Beneficiaries = new SelectList(beneficiaryList, "Id", "Display");
        }
    }
}
