using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Bank.Data;
using Bank.Models;

namespace Bank.Pages.PassBackOperations
{
    public class DetailsModel : PageModel
    {
        private readonly Bank.Data.BankContext _context;

        public DetailsModel(Bank.Data.BankContext context)
        {
            _context = context;
        }

      public PassBackOperation PassBackOperation { get; set; } = default!; 

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.PassBackOperations == null)
            {
                return NotFound();
            }

            var passbackoperation = await _context.PassBackOperations.FirstOrDefaultAsync(m => m.id == id);
            if (passbackoperation == null)
            {
                return NotFound();
            }
            else 
            {
                PassBackOperation = passbackoperation;
            }
            return Page();
        }
    }
}
