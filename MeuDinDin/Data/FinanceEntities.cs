using MeuDinDin.Services;

namespace MeuDinDin.Data;

public sealed class FamilyGroupEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AccessCode { get; set; } = string.Empty;

    public long MonthlyExpenseGoalCents { get; set; }

    public long MonthlyInvestmentGoalCents { get; set; }

    public long CheckingBalanceCents { get; set; }

    public long InvestedBalanceCents { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<FamilyMemberEntity> Members { get; set; } = [];

    public List<CategoryEntity> Categories { get; set; } = [];

    public List<FinancialGoalEntity> Goals { get; set; } = [];
}

public sealed class AppUserEntity
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Guid? FamilyGroupId { get; set; }

    public Guid? FamilyMemberId { get; set; }

    public string Role { get; set; } = "Member";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginUtc { get; set; }

    public FamilyGroupEntity? FamilyGroup { get; set; }
}

public sealed class FamilyMemberEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public long MonthlyIncomeCents { get; set; }

    public long ExpenseGoalCents { get; set; }

    public long InvestmentGoalCents { get; set; }

    public bool IsPrimary { get; set; }

    public FamilyGroupEntity? FamilyGroup { get; set; }
}

public sealed class CategoryEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public CategoryKind Kind { get; set; }

    public string Color { get; set; } = "#1C78EB";

    public bool IsSystem { get; set; }

    public FamilyGroupEntity? FamilyGroup { get; set; }
}

public sealed class IncomeEntryEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public string Description { get; set; } = string.Empty;

    public long AmountCents { get; set; }

    public DateOnly Date { get; set; }

    public RecurrenceType Recurrence { get; set; }

    public EntryStatus Status { get; set; }

    public Guid ResponsibleId { get; set; }

    public Guid CategoryId { get; set; }

    public bool IsExtra { get; set; }
}

public sealed class FixedExpenseEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public string Description { get; set; } = string.Empty;

    public long AmountCents { get; set; }

    public int DueDay { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public EntryStatus Status { get; set; }

    public Guid ResponsibleId { get; set; }

    public Guid CategoryId { get; set; }

    public bool IsPaused { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class VariableExpenseEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public string Description { get; set; } = string.Empty;

    public long AmountCents { get; set; }

    public DateOnly Date { get; set; }

    public ExpensePaymentMethod PaymentMethod { get; set; }

    public EntryStatus Status { get; set; }

    public Guid ResponsibleId { get; set; }

    public Guid CategoryId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class FixedExpenseMonthEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public Guid FixedExpenseId { get; set; }

    public DateOnly Month { get; set; }

    public long? ActualAmountCents { get; set; }

    public bool IsPaid { get; set; }

    public DateOnly? PaidDate { get; set; }
}

public sealed class CreditCardEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public Guid HolderId { get; set; }

    public int DueDay { get; set; }

    public long? LimitCents { get; set; }
}

public sealed class CardPurchaseEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public Guid CardId { get; set; }

    public string Description { get; set; } = string.Empty;

    public long TotalAmountCents { get; set; }

    public int Installments { get; set; }

    public DateOnly PurchaseDate { get; set; }

    public DateOnly FirstDueDate { get; set; }

    public Guid CategoryId { get; set; }

    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;

    public EntryStatus Status { get; set; }
}

public sealed class CardBillPaymentEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public Guid CardId { get; set; }

    public DateOnly Month { get; set; }

    public bool IsPaid { get; set; }

    public DateOnly? PaidDate { get; set; }
}

public sealed class PurchaseSimulationEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public string Description { get; set; } = string.Empty;

    public long TotalAmountCents { get; set; }

    public bool IsInstallment { get; set; }

    public int Installments { get; set; }

    public Guid CardId { get; set; }

    public DateOnly PlannedDate { get; set; }

    public Guid CategoryId { get; set; }

    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class SurplusAllocationEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public DateOnly Month { get; set; }

    public long AmountCents { get; set; }

    public SurplusDestination Destination { get; set; }

    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class FinancialGoalEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public long TargetAmountCents { get; set; }

    public DateOnly? TargetDate { get; set; }

    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public FamilyGroupEntity? FamilyGroup { get; set; }
}

public sealed class GoalContributionEntity
{
    public Guid Id { get; set; }

    public Guid FamilyGroupId { get; set; }

    public Guid GoalId { get; set; }

    public long AmountCents { get; set; }

    public DateOnly Date { get; set; }

    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;
}
