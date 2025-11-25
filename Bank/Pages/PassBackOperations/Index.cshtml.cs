using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bank.Pages.PassBackOperations
{
    public class IndexModel : PageModel
    {
        private readonly Bank.Data.BankContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private int UserId = 0;

        public IndexModel(BankContext context, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _signInManager = signInManager;
            UserId = int.Parse(_signInManager.Context.User.Claims.FirstOrDefault().Value);

        }

        public IList<PassBackOperation> PassBackOperation { get; set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.PassBackOperations != null)
            {
                //  PassBackOperation = await _context.PassBackOperations.ToListAsync();
                PassBackOperation = _context.PassBackOperations.Where(p => p.owner.Id == UserId).ToList();
            }
        }
    }
}
