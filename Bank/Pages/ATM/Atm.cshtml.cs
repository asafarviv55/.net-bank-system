using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace Bank.Pages.ATM
{
    public class AtmModel : PageModel
    {
        private readonly Bank.Data.BankContext _context;

        private readonly ILogger<IndexModel> _logger;
        private const string DEPOSIT = "DEPOSIT";
        private const string WITHDRAW = "WITHDRAW";
        private static readonly HttpClient client = new HttpClient();
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;



        public AtmModel(ILogger<IndexModel> logger, BankContext context, SignInManager<ApplicationUser> signInManager)
        {
            _logger = logger;
            _context = context;
            _signInManager = signInManager;
        }

        public void OnGet()
        {
        }

        [BindProperty]
        public string bankAction { get; set; }

        [BindProperty]
        public string atmAmount { get; set; }


        private String getRandomAlphaNumericString()
        {
            var chars = "1234567890";
            var stringChars = new char[8];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
        }

        public async Task OnPostAsync()
        {
            decimal n1; decimal aAmount = 0;
            if (decimal.TryParse(atmAmount, out n1))
                aAmount = n1;
            var passBackOperation = new PassBackOperation()
            {
                account_balance = aAmount,
                action = bankAction.Equals("Deposit") ? DEPOSIT : WITHDRAW,
                created_at = DateTime.UtcNow,
                due_balance = bankAction.Equals("Deposit") ? 0 : aAmount,
                right_balance = bankAction.Equals("Withdraw") ? aAmount : 0,
                ownerID = int.Parse(_signInManager.Context.User.Claims.FirstOrDefault().Value),
                reference = getRandomAlphaNumericString()
            };
            _context.PassBackOperations.Add(passBackOperation);
            _context.SaveChanges();

            /*  client.DefaultRequestHeaders.Accept.Clear();
              client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
              client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

              var stringTask = client.GetStringAsync("https://localhost:7156/WeatherForecast");

              var msg = await stringTask;
              Console.Write(msg);*/
        }


    }
}
