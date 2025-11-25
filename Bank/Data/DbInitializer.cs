namespace Bank.Data
{
    public class DbInitializer
    {
        public static void Initialize(BankContext context)
        {

            /*********************************//*
            // Look for any Loans.

            if (context.Loans.Any())
            {
                return;   // DB has been seeded
            }

            var loans = new Loan[]
            {
                new Loan{ created_at = DateTime.Now , loan_balance = 1000 }

            };

            context.Loans.AddRange(loans);
            context.SaveChanges();*/


            /**************************************/
            /*********************************//*
            // Look for any PassBackOperations.

            if (context.PassBackOperations.Any())
            {
                return;   // DB has been seeded
            }

            var passBackOperations = new PassBackOperation[]
            {
                new PassBackOperation{ is_charge = true,  account_balance = 1000 , created_at = DateTime.Now}
            };

            context.PassBackOperations.AddRange(passBackOperations);
            context.SaveChanges();

            *//**************************************//*
            // Look for any CreditExpenses.

            if (context.CreditExpenses.Any())
            {
                return;   // DB has been seeded
            }

            var creditExpenses = new CreditExpense[]
            {
                new CreditExpense{ }
            };

            context.CreditExpenses.AddRange(creditExpenses);
            context.SaveChanges();
*/



        }
    }
}
