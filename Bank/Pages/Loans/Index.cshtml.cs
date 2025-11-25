using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Bank.Data;
using Bank.Models;

namespace Bank.Pages.Loans
{
    public class IndexModel : PageModel
    {
        private readonly Bank.Data.BankContext _context;

        public IndexModel(Bank.Data.BankContext context)
        {
            _context = context;
        }

        public IList<Loan> Loan { get;set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.Loans != null)
            {
                Loan = await _context.Loans.ToListAsync();
            }
        }
    }
}
