using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Bank.Data;
using Bank.Models;

namespace Bank.Pages.CreditExpenses
{
    public class DetailsModel : PageModel
    {
        private readonly Bank.Data.BankContext _context;

        public DetailsModel(Bank.Data.BankContext context)
        {
            _context = context;
        }

      public CreditExpense CreditExpense { get; set; } = default!; 

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.CreditExpenses == null)
            {
                return NotFound();
            }

            var creditexpense = await _context.CreditExpenses.FirstOrDefaultAsync(m => m.id == id);
            if (creditexpense == null)
            {
                return NotFound();
            }
            else 
            {
                CreditExpense = creditexpense;
            }
            return Page();
        }
    }
}
