using System.ComponentModel.DataAnnotations;

namespace MeuDinDin.Services;

public enum FamilyViewMode
{
    Family,
    Individual
}

public enum EntryStatus
{
    Planned,
    Paid,
    Cancelled
}

public enum RecurrenceType
{
    None,
    Monthly
}

public enum CategoryKind
{
    Revenue,
    Expense
}

public enum ExpensePaymentMethod
{
    CreditCard,
    Pix,
    Cash
}

public enum SurplusDestination
{
    Investment,
    Reserve,
    CheckingBalance,
    DebtPrepayment,
    ExtraordinaryExpense
}

public sealed record FinanceScope(FamilyViewMode ViewMode, Guid? MemberId);

public sealed record FamilyGroup(
    Guid Id,
    string Name,
    string AccessCode,
    decimal MonthlyExpenseGoal,
    decimal MonthlyInvestmentGoal,
    decimal CheckingBalance,
    decimal InvestedBalance);

public sealed record AppUserProfile(
    Guid Id,
    string DisplayName,
    string Email,
    Guid? FamilyGroupId,
    Guid? FamilyMemberId,
    string Role);

public sealed record FamilyMember(
    Guid Id,
    string Name,
    string Email,
    decimal MonthlyIncome,
    decimal ExpenseGoal,
    decimal InvestmentGoal);

public sealed record Category(
    Guid Id,
    string Name,
    CategoryKind Kind,
    string Color);

public sealed record IncomeEntry(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly Date,
    RecurrenceType Recurrence,
    EntryStatus Status,
    Guid ResponsibleId,
    Guid CategoryId,
    bool IsExtra);

public sealed record FixedExpense(
    Guid Id,
    string Description,
    decimal Amount,
    int DueDay,
    DateOnly StartDate,
    DateOnly? EndDate,
    EntryStatus Status,
    Guid ResponsibleId,
    Guid CategoryId,
    bool IsPaused,
    string Notes);

public sealed record VariableExpense(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly Date,
    ExpensePaymentMethod PaymentMethod,
    Guid ResponsibleId,
    Guid CategoryId,
    string Notes,
    EntryStatus Status);

public sealed record CreditCard(
    Guid Id,
    string Name,
    string Issuer,
    Guid HolderId,
    int DueDay,
    decimal? Limit);

public sealed record CardPurchase(
    Guid Id,
    Guid CardId,
    string Description,
    decimal TotalAmount,
    int Installments,
    DateOnly PurchaseDate,
    DateOnly FirstDueDate,
    Guid CategoryId,
    Guid ResponsibleId,
    string Notes,
    EntryStatus Status);

public sealed record PurchaseSimulation(
    Guid Id,
    string Description,
    decimal TotalAmount,
    bool IsInstallment,
    int Installments,
    Guid CardId,
    DateOnly PlannedDate,
    Guid CategoryId,
    Guid ResponsibleId,
    string Notes);

public sealed record SurplusAllocation(
    Guid Id,
    DateOnly Month,
    decimal Amount,
    SurplusDestination Destination,
    Guid ResponsibleId,
    string Notes);

public sealed record FinancialGoal(
    Guid Id,
    string Name,
    decimal TargetAmount,
    DateOnly? TargetDate,
    Guid ResponsibleId,
    string Notes,
    DateTime CreatedUtc);

public sealed record GoalContribution(
    Guid Id,
    Guid GoalId,
    decimal Amount,
    DateOnly Date,
    Guid ResponsibleId,
    string Notes);

public sealed record MonthlyCommitment(
    DateOnly Month,
    string Label,
    decimal Income,
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal CardExpenses,
    decimal TotalExpenses,
    decimal Surplus,
    decimal ReleasedAmount,
    bool AboveGoal);

public sealed record UpcomingDueItem(
    string Description,
    string Source,
    decimal Amount,
    DateOnly DueDate,
    string ResponsibleName);

public sealed record InstallmentOverview(
    Guid PurchaseId,
    string Description,
    string CardName,
    string ResponsibleName,
    decimal InstallmentAmount,
    int CurrentInstallmentNumber,
    int RemainingInstallments,
    int TotalInstallments,
    DateOnly FinalDueDate,
    string CategoryName,
    bool EndsSoon);

public sealed record AlertItem(
    string Title,
    string Message,
    string Severity);

public sealed record MemberMonthlySummary(
    string Name,
    decimal Income,
    decimal Expenses,
    decimal Surplus);

public sealed record DashboardSnapshot(
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal ImmediateExpenses,
    decimal PaidAmount,
    decimal DueAmount,
    decimal ForecastSurplus,
    decimal AvailableCheckingBalance,
    decimal InvestableAmount,
    decimal InvestedBalance,
    decimal ExpenseGoal,
    decimal ExpenseGoalUsage,
    IReadOnlyList<UpcomingDueItem> UpcomingDueItems,
    IReadOnlyList<InstallmentOverview> EndingSoonInstallments,
    IReadOnlyList<AlertItem> Alerts,
    IReadOnlyList<MemberMonthlySummary> MemberSummaries);

public sealed record ProjectionSnapshot(
    IReadOnlyList<MonthlyCommitment> Months,
    decimal AverageSurplus,
    decimal PeakCommitment,
    decimal TotalReleased);

public sealed record SimulationMonthImpact(
    DateOnly Month,
    string Label,
    decimal AddedExpense,
    decimal ProjectedExpense,
    decimal ProjectedSurplus,
    bool AboveGoal);

public sealed record SimulationInsight(
    decimal CurrentMonthImpact,
    decimal PeakMonthlyImpact,
    bool BreaksGoal,
    DateOnly FirstDueDate,
    IReadOnlyList<SimulationMonthImpact> Months);

public sealed record SavedSimulationSummary(
    Guid Id,
    string Description,
    decimal TotalAmount,
    bool IsInstallment,
    int Installments,
    string CardName,
    string ResponsibleName,
    DateOnly PlannedDate,
    decimal CurrentMonthImpact,
    decimal PeakMonthlyImpact,
    bool BreaksGoal);

public sealed record GoalContributionItem(
    Guid Id,
    decimal Amount,
    DateOnly Date,
    string ResponsibleName,
    string Notes);

public sealed record GoalProgressSummary(
    Guid GoalId,
    string Name,
    decimal TargetAmount,
    decimal SavedAmount,
    decimal RemainingAmount,
    decimal ProgressRatio,
    DateOnly? TargetDate,
    int RemainingMonths,
    decimal SuggestedMonthlyContribution,
    Guid ResponsibleId,
    string ResponsibleName,
    string Notes,
    bool IsCompleted,
    IReadOnlyList<GoalContributionItem> RecentContributions);

public sealed record CardChargeLine(
    Guid PurchaseId,
    string Description,
    string ResponsibleName,
    string CategoryName,
    DateOnly DueDate,
    decimal Amount,
    string InstallmentLabel,
    bool IsFinalInstallment);

public sealed record CardMonthBreakdown(
    DateOnly Month,
    string Label,
    decimal Total,
    IReadOnlyList<CardChargeLine> Charges);

public sealed record CardFinishedPurchase(
    Guid PurchaseId,
    string Description,
    string ResponsibleName,
    string CategoryName,
    DateOnly FinalDueDate,
    decimal ReleasedAmount);

public sealed record CardDetailSnapshot(
    Guid CardId,
    string CardName,
    string Issuer,
    string HolderName,
    int DueDay,
    decimal? Limit,
    decimal PreviousMonthTotal,
    decimal CurrentMonthTotal,
    decimal NextMonthTotal,
    int ActivePurchaseCount,
    int RecentlyFinishedCount,
    decimal ReleasedAmount,
    IReadOnlyList<CardMonthBreakdown> Months,
    IReadOnlyList<CardFinishedPurchase> RecentlyFinished,
    IReadOnlyList<InstallmentOverview> ActiveInstallments);

public sealed record ScopedIncomeItem(
    IncomeEntry Entry,
    FamilyMember Responsible,
    Category Category);

public sealed record ScopedFixedExpenseItem(
    FixedExpense Expense,
    FamilyMember Responsible,
    Category Category);

public sealed record ScopedVariableExpenseItem(
    VariableExpense Expense,
    FamilyMember Responsible,
    Category Category);

public sealed record FixedExpenseMonthStatus(
    Guid ExpenseId,
    string Description,
    string ResponsibleName,
    string CategoryName,
    DateOnly Month,
    DateOnly DueDate,
    decimal EstimatedAmount,
    decimal? ActualAmount,
    decimal EffectiveAmount,
    bool IsPaid);

public sealed record ScopedCardPurchaseItem(
    CardPurchase Purchase,
    FamilyMember Responsible,
    Category Category,
    CreditCard Card,
    decimal InstallmentAmount,
    int CurrentInstallmentNumber,
    int RemainingInstallments,
    DateOnly FinalDueDate);

public sealed record CardBillStatus(
    Guid CardId,
    string CardName,
    DateOnly Month,
    DateOnly DueDate,
    decimal Amount,
    bool IsPaid);

public sealed class OnboardingInput
{
    [Required]
    public string FamilyName { get; set; } = string.Empty;

    [Required]
    public string PrimaryMemberName { get; set; } = string.Empty;

    public string PrimaryMemberEmail { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal PrimaryMemberIncome { get; set; }

    [Required]
    public string PartnerMemberName { get; set; } = string.Empty;

    public string PartnerMemberEmail { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal PartnerMemberIncome { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal MonthlyExpenseGoal { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal MonthlyInvestmentGoal { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal CheckingBalance { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal InvestedBalance { get; set; }
}

public sealed class FamilySettingsInput
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal MonthlyExpenseGoal { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal MonthlyInvestmentGoal { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal CheckingBalance { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal InvestedBalance { get; set; }
}

public sealed class MemberProfileInput
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal MonthlyIncome { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal ExpenseGoal { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal InvestmentGoal { get; set; }
}

public sealed class CategoryInput
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public CategoryKind Kind { get; set; } = CategoryKind.Expense;

    [Required]
    public string Color { get; set; } = "#1C78EB";
}

public sealed class CreditCardInput
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public Guid HolderId { get; set; }

    [Range(1, 31)]
    public int DueDay { get; set; } = 10;

    public decimal? Limit { get; set; }
}

public sealed class IncomeEntryInput
{
    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public RecurrenceType Recurrence { get; set; } = RecurrenceType.Monthly;

    public EntryStatus Status { get; set; } = EntryStatus.Planned;

    [Required]
    public Guid ResponsibleId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public bool IsExtra { get; set; }
}

public sealed class IncomeEntryEditInput
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public RecurrenceType Recurrence { get; set; } = RecurrenceType.Monthly;

    public EntryStatus Status { get; set; } = EntryStatus.Planned;

    [Required]
    public Guid ResponsibleId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public bool IsExtra { get; set; }
}

public sealed class FixedExpenseInput
{
    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    [Range(1, 31)]
    public int DueDay { get; set; } = DateTime.Today.Day;

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly? EndDate { get; set; }

    public EntryStatus Status { get; set; } = EntryStatus.Planned;

    [Required]
    public Guid ResponsibleId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class FixedExpenseEditInput
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    [Range(1, 31)]
    public int DueDay { get; set; } = DateTime.Today.Day;

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly? EndDate { get; set; }

    public EntryStatus Status { get; set; } = EntryStatus.Planned;

    [Required]
    public Guid ResponsibleId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class FixedExpenseMonthUpdateInput
{
    [Required]
    public Guid ExpenseId { get; set; }

    public DateOnly Month { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public decimal? ActualAmount { get; set; }

    public bool IsPaid { get; set; }
}

public sealed class CardPurchaseInput
{
    [Required]
    public Guid CardId { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal TotalAmount { get; set; }

    [Range(1, 48)]
    public int Installments { get; set; } = 1;

    public DateOnly PurchaseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly FirstDueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;

    public EntryStatus Status { get; set; } = EntryStatus.Planned;
}

public sealed class VariableExpenseInput
{
    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Pix;

    [Required]
    public Guid ResponsibleId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public string Notes { get; set; } = string.Empty;

    public EntryStatus Status { get; set; } = EntryStatus.Paid;
}

public sealed class CardPurchaseEditInput
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid CardId { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal TotalAmount { get; set; }

    [Range(1, 48)]
    public int Installments { get; set; } = 1;

    public DateOnly PurchaseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly FirstDueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;

    public EntryStatus Status { get; set; } = EntryStatus.Planned;
}

public sealed class CardBillPaymentInput
{
    [Required]
    public Guid CardId { get; set; }

    public DateOnly Month { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public bool IsPaid { get; set; }
}

public sealed class PurchaseSimulationInput
{
    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal TotalAmount { get; set; }

    public bool IsInstallment { get; set; } = true;

    [Range(1, 48)]
    public int Installments { get; set; } = 1;

    [Required]
    public Guid CardId { get; set; }

    public DateOnly PlannedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class GoalInput
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal TargetAmount { get; set; }

    public DateOnly? TargetDate { get; set; }

    [Required]
    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class GoalEditInput
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal TargetAmount { get; set; }

    public DateOnly? TargetDate { get; set; }

    [Required]
    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class GoalContributionInput
{
    [Required]
    public Guid GoalId { get; set; }

    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public Guid ResponsibleId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public interface IFinanceHub
{
    event Action? StateChanged;

    bool IsAuthenticated { get; }

    bool HasFamilySetup { get; }

    FamilyViewMode ViewMode { get; }

    Guid SelectedMemberId { get; }

    AppUserProfile? CurrentUser { get; }

    FamilyGroup Family { get; }

    string FamilyAccessCode { get; }

    IReadOnlyList<FamilyMember> Members { get; }

    IReadOnlyList<Category> Categories { get; }

    IReadOnlyList<CreditCard> Cards { get; }

    IReadOnlyList<SurplusAllocation> Allocations { get; }

    FinanceScope CurrentScope { get; }

    void SetActiveUser(Guid userId, string email, string displayName);

    void ClearActiveUser();

    DateOnly GetPlanningReferenceDate(DateOnly referenceDate);

    void SetViewMode(FamilyViewMode viewMode);

    void SetSelectedMember(Guid memberId);

    void CompleteOnboarding(OnboardingInput input);

    void UpdateFamily(FamilySettingsInput input);

    void UpdateMember(MemberProfileInput input);

    void AddCategory(CategoryInput input);

    void AddCreditCard(CreditCardInput input);

    IReadOnlyList<ScopedIncomeItem> GetIncomeEntries();

    IReadOnlyList<ScopedFixedExpenseItem> GetFixedExpenses();

    IReadOnlyList<ScopedVariableExpenseItem> GetVariableExpenses();

    IReadOnlyList<FixedExpenseMonthStatus> GetFixedExpenseMonthStatuses(DateOnly month);

    IReadOnlyList<ScopedCardPurchaseItem> GetCardPurchases();

    CardBillStatus? GetCardBillStatus(Guid cardId, DateOnly month);

    CardDetailSnapshot? GetCardDetails(Guid cardId, DateOnly referenceDate);

    IReadOnlyList<InstallmentOverview> GetInstallmentOverview(DateOnly referenceDate);

    IReadOnlyList<SavedSimulationSummary> GetSimulationSummaries(DateOnly referenceDate);

    IReadOnlyList<GoalProgressSummary> GetGoalProgress(DateOnly referenceDate);

    DashboardSnapshot GetDashboard(DateOnly referenceDate);

    ProjectionSnapshot GetProjection(DateOnly startMonth, int months = 12);

    SimulationInsight AnalyzeSimulation(PurchaseSimulationInput input, DateOnly referenceDate, int months = 12);

    DateOnly GetSuggestedFirstDueDate(Guid cardId, DateOnly purchaseDate);

    void AddIncome(IncomeEntryInput input);

    void UpdateIncome(IncomeEntryEditInput input);

    void RemoveIncome(Guid incomeId);

    void AddFixedExpense(FixedExpenseInput input);

    void AddVariableExpense(VariableExpenseInput input);

    void UpdateFixedExpense(FixedExpenseEditInput input);

    void UpdateFixedExpenseMonth(FixedExpenseMonthUpdateInput input);

    void PauseFixedExpense(Guid expenseId);

    void RemoveFixedExpense(Guid expenseId);

    void AddCardPurchase(CardPurchaseInput input);

    void UpdateCardPurchase(CardPurchaseEditInput input);

    void RemoveCardPurchase(Guid purchaseId);

    void SetCardBillPayment(CardBillPaymentInput input);

    void AddGoal(GoalInput input);

    void UpdateGoal(GoalEditInput input);

    void RemoveGoal(Guid goalId);

    void AddGoalContribution(GoalContributionInput input);

    void SaveSimulation(PurchaseSimulationInput input);

    void RemoveSimulation(Guid simulationId);

    void ConvertSimulationToPurchase(Guid simulationId);
}
