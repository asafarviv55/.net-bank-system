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

    // Existing
    public DbSet<Loan> Loans { get; set; }
    public DbSet<CreditExpense> CreditExpenses { get; set; }
    public DbSet<PassBackOperation> PassBackOperations { get; set; }

    // Banking Features
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Beneficiary> Beneficiaries { get; set; }
    public DbSet<ScheduledPayment> ScheduledPayments { get; set; }
    public DbSet<BillPayment> BillPayments { get; set; }
    public DbSet<LoanApplication> LoanApplications { get; set; }
    public DbSet<AccountStatement> AccountStatements { get; set; }
    public DbSet<SpendingCategory> SpendingCategories { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<SpendingReport> SpendingReports { get; set; }

    // New Features
    public DbSet<Card> Cards { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<CurrencyExchange> CurrencyExchanges { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SecurityEvent> SecurityEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Existing tables
        modelBuilder.Entity<Loan>().ToTable("Loan");
        modelBuilder.Entity<CreditExpense>().ToTable("CreditExpense");
        modelBuilder.Entity<PassBackOperation>().ToTable("PassBackOperation");

        // Banking tables
        modelBuilder.Entity<Account>().ToTable("Account");
        modelBuilder.Entity<Transaction>().ToTable("Transaction");
        modelBuilder.Entity<Beneficiary>().ToTable("Beneficiary");
        modelBuilder.Entity<ScheduledPayment>().ToTable("ScheduledPayment");
        modelBuilder.Entity<BillPayment>().ToTable("BillPayment");
        modelBuilder.Entity<LoanApplication>().ToTable("LoanApplication");
        modelBuilder.Entity<AccountStatement>().ToTable("AccountStatement");
        modelBuilder.Entity<SpendingCategory>().ToTable("SpendingCategory");
        modelBuilder.Entity<Budget>().ToTable("Budget");
        modelBuilder.Entity<SpendingReport>().ToTable("SpendingReport");

        // New feature tables
        modelBuilder.Entity<Card>().ToTable("Card");
        modelBuilder.Entity<Notification>().ToTable("Notification");
        modelBuilder.Entity<NotificationPreference>().ToTable("NotificationPreference");
        modelBuilder.Entity<CurrencyExchange>().ToTable("CurrencyExchange");
        modelBuilder.Entity<ExchangeRate>().ToTable("ExchangeRate");
        modelBuilder.Entity<AuditLog>().ToTable("AuditLog");
        modelBuilder.Entity<SecurityEvent>().ToTable("SecurityEvent");

        // Account relationships
        modelBuilder.Entity<Account>()
            .HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction relationships
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.DestinationAccount)
            .WithMany()
            .HasForeignKey(t => t.DestinationAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Beneficiary relationships
        modelBuilder.Entity<Beneficiary>()
            .HasOne(b => b.Account)
            .WithMany(a => a.Beneficiaries)
            .HasForeignKey(b => b.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // ScheduledPayment relationships
        modelBuilder.Entity<ScheduledPayment>()
            .HasOne(s => s.Account)
            .WithMany(a => a.ScheduledPayments)
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScheduledPayment>()
            .HasOne(s => s.DestinationAccount)
            .WithMany()
            .HasForeignKey(s => s.DestinationAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // LoanApplication relationships
        modelBuilder.Entity<LoanApplication>()
            .HasOne(l => l.User)
            .WithMany(u => u.LoanApplications)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraints
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.ReferenceNumber)
            .IsUnique();

        modelBuilder.Entity<LoanApplication>()
            .HasIndex(l => l.ApplicationNumber)
            .IsUnique();

        // Card relationships
        modelBuilder.Entity<Card>()
            .HasOne(c => c.Account)
            .WithMany()
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Card>()
            .HasIndex(c => c.CardNumber)
            .IsUnique();

        // Notification relationships
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Currency exchange relationships
        modelBuilder.Entity<CurrencyExchange>()
            .HasOne(e => e.SourceAccount)
            .WithMany()
            .HasForeignKey(e => e.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CurrencyExchange>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CurrencyExchange>()
            .HasIndex(e => e.ReferenceNumber)
            .IsUnique();

        // Exchange rate unique constraint
        modelBuilder.Entity<ExchangeRate>()
            .HasIndex(er => new { er.BaseCurrency, er.TargetCurrency })
            .IsUnique();

        // Seed default spending categories
        modelBuilder.Entity<SpendingCategory>().HasData(
            new SpendingCategory { Id = 1, Name = "Groceries", Color = "#4CAF50", Icon = "shopping_cart", IsSystem = true },
            new SpendingCategory { Id = 2, Name = "Utilities", Color = "#2196F3", Icon = "bolt", IsSystem = true },
            new SpendingCategory { Id = 3, Name = "Transportation", Color = "#FF9800", Icon = "directions_car", IsSystem = true },
            new SpendingCategory { Id = 4, Name = "Entertainment", Color = "#9C27B0", Icon = "movie", IsSystem = true },
            new SpendingCategory { Id = 5, Name = "Dining", Color = "#E91E63", Icon = "restaurant", IsSystem = true },
            new SpendingCategory { Id = 6, Name = "Healthcare", Color = "#00BCD4", Icon = "local_hospital", IsSystem = true },
            new SpendingCategory { Id = 7, Name = "Shopping", Color = "#795548", Icon = "shopping_bag", IsSystem = true },
            new SpendingCategory { Id = 8, Name = "Travel", Color = "#607D8B", Icon = "flight", IsSystem = true },
            new SpendingCategory { Id = 9, Name = "Education", Color = "#3F51B5", Icon = "school", IsSystem = true },
            new SpendingCategory { Id = 10, Name = "Other", Color = "#9E9E9E", Icon = "category", IsSystem = true }
        );

        // Seed default exchange rates
        modelBuilder.Entity<ExchangeRate>().HasData(
            new ExchangeRate { Id = 1, BaseCurrency = "USD", TargetCurrency = "EUR", Rate = 0.92m, BuySpread = 0.02m, SellSpread = 0.02m, IsActive = true },
            new ExchangeRate { Id = 2, BaseCurrency = "USD", TargetCurrency = "GBP", Rate = 0.79m, BuySpread = 0.02m, SellSpread = 0.02m, IsActive = true },
            new ExchangeRate { Id = 3, BaseCurrency = "USD", TargetCurrency = "JPY", Rate = 149.50m, BuySpread = 0.02m, SellSpread = 0.02m, IsActive = true },
            new ExchangeRate { Id = 4, BaseCurrency = "USD", TargetCurrency = "CAD", Rate = 1.36m, BuySpread = 0.02m, SellSpread = 0.02m, IsActive = true },
            new ExchangeRate { Id = 5, BaseCurrency = "USD", TargetCurrency = "AUD", Rate = 1.52m, BuySpread = 0.02m, SellSpread = 0.02m, IsActive = true },
            new ExchangeRate { Id = 6, BaseCurrency = "EUR", TargetCurrency = "USD", Rate = 1.09m, BuySpread = 0.02m, SellSpread = 0.02m, IsActive = true },
            new ExchangeRate { Id = 7, BaseCurrency = "GBP", TargetCurrency = "USD", Rate = 1.27m, BuySpread = 0.02m, SellSpread = 0.02m, IsActive = true },
            new ExchangeRate { Id = 8, BaseCurrency = "JPY", TargetCurrency = "USD", Rate = 0.0067m, BuySpread = 0.02m, SellSpread = 0.02m, IsActive = true }
        );
    }
}
