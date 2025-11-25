using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bank.Data;

public class BankContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public BankContext(DbContextOptions<BankContext> options)
        : base(options)
    {
    }

    public DbSet<Loan> Loans { get; set; }

    public DbSet<CreditExpense> CreditExpenses { get; set; }

    public DbSet<PassBackOperation> PassBackOperations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Loan>().ToTable("Loan");
        modelBuilder.Entity<CreditExpense>().ToTable("CreditExpense");
        modelBuilder.Entity<PassBackOperation>().ToTable("PassBackOperation");
    }
}
