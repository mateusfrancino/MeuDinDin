using Microsoft.EntityFrameworkCore;

namespace MeuDinDin.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FamilyGroupEntity> FamilyGroups => Set<FamilyGroupEntity>();

    public DbSet<AppUserEntity> AppUsers => Set<AppUserEntity>();

    public DbSet<FamilyMemberEntity> FamilyMembers => Set<FamilyMemberEntity>();

    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();

    public DbSet<IncomeEntryEntity> IncomeEntries => Set<IncomeEntryEntity>();

    public DbSet<FixedExpenseEntity> FixedExpenses => Set<FixedExpenseEntity>();

    public DbSet<VariableExpenseEntity> VariableExpenses => Set<VariableExpenseEntity>();

    public DbSet<FixedExpenseMonthEntity> FixedExpenseMonths => Set<FixedExpenseMonthEntity>();

    public DbSet<CreditCardEntity> CreditCards => Set<CreditCardEntity>();

    public DbSet<CardPurchaseEntity> CardPurchases => Set<CardPurchaseEntity>();

    public DbSet<CardBillPaymentEntity> CardBillPayments => Set<CardBillPaymentEntity>();

    public DbSet<PurchaseSimulationEntity> PurchaseSimulations => Set<PurchaseSimulationEntity>();

    public DbSet<SurplusAllocationEntity> SurplusAllocations => Set<SurplusAllocationEntity>();

    public DbSet<FinancialGoalEntity> FinancialGoals => Set<FinancialGoalEntity>();

    public DbSet<GoalContributionEntity> GoalContributions => Set<GoalContributionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FamilyGroupEntity>(entity =>
        {
            entity.ToTable("family_groups");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(160);
            entity.Property(item => item.AccessCode).HasMaxLength(12);
            entity.HasIndex(item => item.AccessCode).IsUnique();
        });

        modelBuilder.Entity<AppUserEntity>(entity =>
        {
            entity.ToTable("app_users");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Email).HasMaxLength(180);
            entity.Property(item => item.NormalizedEmail).HasMaxLength(180);
            entity.Property(item => item.DisplayName).HasMaxLength(120);
            entity.Property(item => item.PasswordHash).HasMaxLength(1024);
            entity.Property(item => item.Role).HasMaxLength(24);
            entity.HasIndex(item => item.NormalizedEmail).IsUnique();
            entity.HasOne(item => item.FamilyGroup)
                .WithMany()
                .HasForeignKey(item => item.FamilyGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FamilyMemberEntity>(entity =>
        {
            entity.ToTable("family_members");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(120);
            entity.Property(item => item.Email).HasMaxLength(180);
            entity.HasOne(item => item.FamilyGroup)
                .WithMany(item => item.Members)
                .HasForeignKey(item => item.FamilyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(120);
            entity.Property(item => item.Color).HasMaxLength(20);
            entity.HasOne(item => item.FamilyGroup)
                .WithMany(item => item.Categories)
                .HasForeignKey(item => item.FamilyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IncomeEntryEntity>(entity =>
        {
            entity.ToTable("income_entries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Description).HasMaxLength(160);
        });

        modelBuilder.Entity<FixedExpenseEntity>(entity =>
        {
            entity.ToTable("fixed_expenses");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Description).HasMaxLength(160);
            entity.Property(item => item.Notes).HasMaxLength(320);
        });

        modelBuilder.Entity<VariableExpenseEntity>(entity =>
        {
            entity.ToTable("variable_expenses");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Description).HasMaxLength(160);
            entity.Property(item => item.Notes).HasMaxLength(320);
        });

        modelBuilder.Entity<FixedExpenseMonthEntity>(entity =>
        {
            entity.ToTable("fixed_expense_months");
            entity.HasKey(item => item.Id);
        });

        modelBuilder.Entity<CreditCardEntity>(entity =>
        {
            entity.ToTable("credit_cards");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(120);
            entity.Property(item => item.Issuer).HasMaxLength(120);
        });

        modelBuilder.Entity<CardPurchaseEntity>(entity =>
        {
            entity.ToTable("card_purchases");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Description).HasMaxLength(160);
            entity.Property(item => item.Notes).HasMaxLength(320);
        });

        modelBuilder.Entity<CardBillPaymentEntity>(entity =>
        {
            entity.ToTable("card_bill_payments");
            entity.HasKey(item => item.Id);
        });

        modelBuilder.Entity<PurchaseSimulationEntity>(entity =>
        {
            entity.ToTable("purchase_simulations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Description).HasMaxLength(160);
            entity.Property(item => item.Notes).HasMaxLength(320);
        });

        modelBuilder.Entity<SurplusAllocationEntity>(entity =>
        {
            entity.ToTable("surplus_allocations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Notes).HasMaxLength(320);
        });

        modelBuilder.Entity<FinancialGoalEntity>(entity =>
        {
            entity.ToTable("financial_goals");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(140);
            entity.Property(item => item.Notes).HasMaxLength(320);
            entity.HasOne(item => item.FamilyGroup)
                .WithMany(item => item.Goals)
                .HasForeignKey(item => item.FamilyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GoalContributionEntity>(entity =>
        {
            entity.ToTable("goal_contributions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Notes).HasMaxLength(320);
        });

        base.OnModelCreating(modelBuilder);
    }
}
