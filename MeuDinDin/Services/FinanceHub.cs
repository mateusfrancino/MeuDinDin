using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MeuDinDin.Data;

namespace MeuDinDin.Services;

public sealed partial class FinanceHub : IFinanceHub
{
    private static readonly FamilyGroup EmptyFamily = new(Guid.Empty, string.Empty, string.Empty, 0m, 0m, 0m, 0m);

    private readonly IDbContextFactory<AppDbContext> dbContextFactory;
    private readonly CultureInfo culture = CultureInfo.GetCultureInfo("pt-BR");
    private List<FamilyMember> members = [];
    private List<Category> categories = [];
    private List<IncomeEntry> incomes = [];
    private List<FixedExpense> fixedExpenses = [];
    private List<VariableExpense> variableExpenses = [];
    private List<CreditCard> cards = [];
    private List<CardPurchase> purchases = [];
    private List<PurchaseSimulation> simulations = [];
    private List<SurplusAllocation> allocations = [];
    private FamilyGroup family = EmptyFamily;
    private AppUserProfile? currentUser;

    public FinanceHub(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public event Action? StateChanged;

    public bool IsAuthenticated => currentUser is not null;

    public bool HasFamilySetup => family.Id != Guid.Empty && members.Count > 0;

    public FamilyViewMode ViewMode { get; private set; } = FamilyViewMode.Family;

    public Guid SelectedMemberId { get; private set; }

    public AppUserProfile? CurrentUser => currentUser;

    public FamilyGroup Family => family;

    public string FamilyAccessCode => family.AccessCode;

    public IReadOnlyList<FamilyMember> Members => members;

    public IReadOnlyList<Category> Categories => categories;

    public IReadOnlyList<CreditCard> Cards => cards;

    public IReadOnlyList<SurplusAllocation> Allocations => allocations;

    public FinanceScope CurrentScope =>
        ViewMode == FamilyViewMode.Family || SelectedMemberId == Guid.Empty
            ? new FinanceScope(FamilyViewMode.Family, null)
            : new FinanceScope(ViewMode, SelectedMemberId);

    public void SetActiveUser(Guid userId, string email, string displayName)
    {
        if (currentUser?.Id == userId)
        {
            return;
        }

        currentUser = new AppUserProfile(userId, displayName, email, null, null, string.Empty);
        ReloadState();
        NotifyStateChanged();
    }

    public void ClearActiveUser()
    {
        if (currentUser is null && family.Id == Guid.Empty)
        {
            return;
        }

        currentUser = null;
        ResetState();
        NotifyStateChanged();
    }

    public DateOnly GetPlanningReferenceDate(DateOnly referenceDate) =>
        ResolveReferenceDate(referenceDate);

    public void SetViewMode(FamilyViewMode viewMode)
    {
        ViewMode = viewMode;
        NotifyStateChanged();
    }

    public void SetSelectedMember(Guid memberId)
    {
        if (members.All(member => member.Id != memberId))
        {
            return;
        }

        SelectedMemberId = memberId;
        NotifyStateChanged();
    }

    public void CompleteOnboarding(OnboardingInput input)
    {
        if (HasFamilySetup || currentUser is null)
        {
            return;
        }

        var familyId = Guid.NewGuid();
        var primaryMemberId = Guid.NewGuid();
        var partnerMemberId = Guid.NewGuid();
        var splitExpenseGoal = input.MonthlyExpenseGoal / 2m;
        var splitInvestmentGoal = input.MonthlyInvestmentGoal / 2m;

        using var db = dbContextFactory.CreateDbContext();
        var userEntity = db.AppUsers.FirstOrDefault(item => item.Id == currentUser.Id);
        if (userEntity is null || userEntity.FamilyGroupId is not null)
        {
            ReloadState();
            return;
        }

        db.FamilyGroups.Add(new FamilyGroupEntity
        {
            Id = familyId,
            Name = input.FamilyName.Trim(),
            MonthlyExpenseGoalCents = ToCents(input.MonthlyExpenseGoal),
            MonthlyInvestmentGoalCents = ToCents(input.MonthlyInvestmentGoal),
            CheckingBalanceCents = ToCents(input.CheckingBalance),
            InvestedBalanceCents = ToCents(input.InvestedBalance)
        });

        db.FamilyMembers.AddRange(
        [
            new FamilyMemberEntity
            {
                Id = primaryMemberId,
                FamilyGroupId = familyId,
                Name = input.PrimaryMemberName.Trim(),
                Email = input.PrimaryMemberEmail.Trim(),
                MonthlyIncomeCents = ToCents(input.PrimaryMemberIncome),
                ExpenseGoalCents = ToCents(splitExpenseGoal),
                InvestmentGoalCents = ToCents(splitInvestmentGoal),
                IsPrimary = true
            },
            new FamilyMemberEntity
            {
                Id = partnerMemberId,
                FamilyGroupId = familyId,
                Name = input.PartnerMemberName.Trim(),
                Email = input.PartnerMemberEmail.Trim(),
                MonthlyIncomeCents = ToCents(input.PartnerMemberIncome),
                ExpenseGoalCents = ToCents(splitExpenseGoal),
                InvestmentGoalCents = ToCents(splitInvestmentGoal),
                IsPrimary = false
            }
        ]);

        userEntity.DisplayName = input.PrimaryMemberName.Trim();
        userEntity.FamilyGroupId = familyId;
        userEntity.FamilyMemberId = primaryMemberId;
        userEntity.Role = "Owner";

        db.Categories.AddRange(FamilySeedData.BuildDefaultCategories(familyId));
        db.SaveChanges();

        ReloadState();
        SelectedMemberId = primaryMemberId;
        ViewMode = FamilyViewMode.Family;
        NotifyStateChanged();
    }

    public void UpdateFamily(FamilySettingsInput input)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.FamilyGroups.FirstOrDefault(item => item.Id == family.Id);
        if (entity is null)
        {
            return;
        }

        entity.Name = input.Name.Trim();
        entity.MonthlyExpenseGoalCents = ToCents(input.MonthlyExpenseGoal);
        entity.MonthlyInvestmentGoalCents = ToCents(input.MonthlyInvestmentGoal);
        entity.CheckingBalanceCents = ToCents(input.CheckingBalance);
        entity.InvestedBalanceCents = ToCents(input.InvestedBalance);
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void UpdateMember(MemberProfileInput input)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.FamilyMembers.FirstOrDefault(item => item.Id == input.Id && item.FamilyGroupId == family.Id);
        if (entity is null)
        {
            return;
        }

        entity.Name = input.Name.Trim();
        entity.Email = input.Email.Trim();
        entity.MonthlyIncomeCents = ToCents(input.MonthlyIncome);
        entity.ExpenseGoalCents = ToCents(input.ExpenseGoal);
        entity.InvestmentGoalCents = ToCents(input.InvestmentGoal);
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void AddCategory(CategoryInput input)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        var normalizedName = input.Name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var alreadyExists = db.Categories.Any(item =>
            item.FamilyGroupId == family.Id &&
            item.Kind == input.Kind &&
            item.Name.ToLower() == normalizedName.ToLower());

        if (alreadyExists)
        {
            return;
        }

        db.Categories.Add(new CategoryEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            Name = normalizedName,
            Kind = input.Kind,
            Color = input.Color.Trim(),
            IsSystem = false
        });
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void AddCreditCard(CreditCardInput input)
    {
        if (!HasFamilySetup || members.All(item => item.Id != input.HolderId))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        db.CreditCards.Add(new CreditCardEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            Name = input.Name.Trim(),
            Issuer = input.Issuer.Trim(),
            HolderId = input.HolderId,
            DueDay = input.DueDay,
            LimitCents = input.Limit is null ? null : ToCents(input.Limit.Value)
        });
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public IReadOnlyList<ScopedIncomeItem> GetIncomeEntries()
    {
        if (!HasFamilySetup)
        {
            return [];
        }

        return FilterScope(incomes, entry => entry.ResponsibleId)
            .OrderByDescending(entry => entry.Date)
            .Select(entry => new ScopedIncomeItem(
                entry,
                GetMember(entry.ResponsibleId),
                GetCategory(entry.CategoryId)))
            .ToList();
    }

    public IReadOnlyList<ScopedFixedExpenseItem> GetFixedExpenses()
    {
        if (!HasFamilySetup)
        {
            return [];
        }

        return FilterScope(fixedExpenses.Where(expense => expense.Status != EntryStatus.Cancelled), entry => entry.ResponsibleId)
            .OrderBy(expense => expense.IsPaused)
            .ThenBy(expense => expense.DueDay)
            .Select(expense => new ScopedFixedExpenseItem(
                expense,
                GetMember(expense.ResponsibleId),
                GetCategory(expense.CategoryId)))
            .ToList();
    }

    public IReadOnlyList<ScopedVariableExpenseItem> GetVariableExpenses()
    {
        if (!HasFamilySetup)
        {
            return [];
        }

        return FilterScope(variableExpenses.Where(expense => expense.Status != EntryStatus.Cancelled), entry => entry.ResponsibleId)
            .OrderByDescending(expense => expense.Date)
            .ThenByDescending(expense => expense.Amount)
            .Select(expense => new ScopedVariableExpenseItem(
                expense,
                GetMember(expense.ResponsibleId),
                GetCategory(expense.CategoryId)))
            .ToList();
    }

    public IReadOnlyList<ScopedCardPurchaseItem> GetCardPurchases()
    {
        if (!HasFamilySetup)
        {
            return [];
        }

        var today = ResolveReferenceDate(DateOnly.FromDateTime(DateTime.Today));
        return FilterScope(purchases.Where(purchase => purchase.Status != EntryStatus.Cancelled), entry => entry.ResponsibleId)
            .OrderByDescending(purchase => purchase.PurchaseDate)
            .Select(purchase => new ScopedCardPurchaseItem(
                purchase,
                GetMember(purchase.ResponsibleId),
                GetCategory(purchase.CategoryId),
                GetCard(purchase.CardId),
                GetInstallmentAmount(purchase, 1),
                GetCurrentInstallmentNumber(purchase, today),
                GetRemainingInstallments(purchase, today),
                GetFinalDueDate(purchase)))
            .ToList();
    }

    public IReadOnlyList<InstallmentOverview> GetInstallmentOverview(DateOnly referenceDate)
    {
        if (!HasFamilySetup)
        {
            return [];
        }

        referenceDate = ResolveReferenceDate(referenceDate);

        return FilterScope(purchases.Where(purchase => purchase.Status != EntryStatus.Cancelled), entry => entry.ResponsibleId)
            .Select(purchase =>
            {
                var card = GetCard(purchase.CardId);
                var member = GetMember(purchase.ResponsibleId);
                var finalDueDate = GetFinalDueDate(purchase);

                return new InstallmentOverview(
                    purchase.Id,
                    purchase.Description,
                    card.Name,
                    member.Name,
                    GetInstallmentAmount(purchase, 1),
                    GetCurrentInstallmentNumber(purchase, referenceDate),
                    GetRemainingInstallments(purchase, referenceDate),
                    purchase.Installments,
                    finalDueDate,
                    GetCategory(purchase.CategoryId).Name,
                    finalDueDate <= referenceDate.AddMonths(2));
            })
            .Where(item => item.FinalDueDate >= StartOfMonth(referenceDate))
            .OrderBy(item => item.FinalDueDate)
            .ToList();
    }

    public IReadOnlyList<SavedSimulationSummary> GetSimulationSummaries(DateOnly referenceDate)
    {
        if (!HasFamilySetup)
        {
            return [];
        }

        referenceDate = ResolveReferenceDate(referenceDate);

        return FilterScope(simulations, entry => entry.ResponsibleId)
            .OrderByDescending(item => item.PlannedDate)
            .Select(simulation =>
            {
                var insight = AnalyzeSimulationCore(
                    new PurchaseSimulationInput
                    {
                        Description = simulation.Description,
                        TotalAmount = simulation.TotalAmount,
                        IsInstallment = simulation.IsInstallment,
                        Installments = simulation.Installments,
                        CardId = simulation.CardId,
                        PlannedDate = simulation.PlannedDate,
                        CategoryId = simulation.CategoryId,
                        ResponsibleId = simulation.ResponsibleId,
                        Notes = simulation.Notes
                    },
                    referenceDate,
                    12);

                return new SavedSimulationSummary(
                    simulation.Id,
                    simulation.Description,
                    simulation.TotalAmount,
                    simulation.IsInstallment,
                    simulation.Installments,
                    SafeCardName(simulation.CardId),
                    SafeMemberName(simulation.ResponsibleId),
                    simulation.PlannedDate,
                    insight.CurrentMonthImpact,
                    insight.PeakMonthlyImpact,
                    insight.BreaksGoal);
            })
            .ToList();
    }

    public DashboardSnapshot GetDashboard(DateOnly referenceDate)
    {
        if (!HasFamilySetup)
        {
            return new DashboardSnapshot(0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, [], [], [], []);
        }

        referenceDate = ResolveReferenceDate(referenceDate);
        var month = StartOfMonth(referenceDate);
        var incomeTotal = GetMonthlyIncome(month);
        var fixedOccurrences = GetFixedExpenseOccurrences(month).ToList();
        var variableOccurrences = GetVariableExpenseOccurrences(month).ToList();
        var cardOccurrences = GetCardPurchaseOccurrences(month).ToList();
        var immediateExpenses = variableOccurrences.Sum(item => item.Amount);
        var expenseTotal = fixedOccurrences.Sum(item => item.Amount) + immediateExpenses + cardOccurrences.Sum(item => item.Amount);
        var paidAmount = fixedOccurrences.Where(item => item.IsPaid).Sum(item => item.Amount)
            + immediateExpenses
            + cardOccurrences.Where(item => item.IsPaid).Sum(item => item.Amount);
        var dueAmount = expenseTotal - paidAmount;
        var expenseGoal = GetScopeExpenseGoal();
        var currentMonthInvestment = allocations
            .Where(allocation => IsInScope(allocation.ResponsibleId))
            .Where(allocation => allocation.Destination == SurplusDestination.Investment)
            .Where(allocation => allocation.Month.Year == month.Year && allocation.Month.Month == month.Month)
            .Sum(allocation => allocation.Amount);
        var availableCheckingBalance = family.CheckingBalance + incomeTotal - immediateExpenses;

        return new DashboardSnapshot(
            incomeTotal,
            expenseTotal,
            immediateExpenses,
            paidAmount,
            dueAmount,
            incomeTotal - expenseTotal,
            availableCheckingBalance,
            Math.Max(incomeTotal - expenseTotal, 0m),
            family.InvestedBalance + currentMonthInvestment,
            expenseGoal,
            expenseGoal == 0 ? 0 : expenseTotal / expenseGoal,
            GetUpcomingDueItems(referenceDate),
            GetInstallmentOverview(referenceDate).Where(item => item.EndsSoon).Take(5).ToList(),
            GetAlerts(referenceDate, expenseTotal, expenseGoal),
            GetMemberSummaries(month));
    }

    public ProjectionSnapshot GetProjection(DateOnly startMonth, int months = 12)
    {
        if (!HasFamilySetup)
        {
            return new ProjectionSnapshot([], 0m, 0m, 0m);
        }

        startMonth = ResolveReferenceDate(startMonth);
        var firstMonth = StartOfMonth(startMonth);
        var expenseGoal = GetScopeExpenseGoal();
        var commitments = Enumerable.Range(0, months)
            .Select(offset =>
            {
                var month = StartOfMonth(firstMonth.AddMonths(offset));
                var income = GetMonthlyIncome(month);
                var fixedTotal = GetFixedExpenseOccurrences(month).Sum(item => item.Amount);
                var variableTotal = GetVariableExpenseOccurrences(month).Sum(item => item.Amount);
                var cardTotal = GetCardPurchaseOccurrences(month).Sum(item => item.Amount);
                var total = fixedTotal + variableTotal + cardTotal;

                return new MonthlyCommitment(
                    month,
                    month.ToString("MMM/yy", culture),
                    income,
                    fixedTotal,
                    variableTotal,
                    cardTotal,
                    total,
                    income - total,
                    GetReleasedAmount(month),
                    total > expenseGoal);
            })
            .ToList();

        return new ProjectionSnapshot(
            commitments,
            commitments.Count == 0 ? 0 : commitments.Average(item => item.Surplus),
            commitments.Count == 0 ? 0 : commitments.Max(item => item.TotalExpenses),
            commitments.Sum(item => item.ReleasedAmount));
    }

    public SimulationInsight AnalyzeSimulation(PurchaseSimulationInput input, DateOnly referenceDate, int months = 12)
    {
        if (!HasFamilySetup || cards.All(card => card.Id != input.CardId))
        {
            return EmptySimulation(referenceDate);
        }

        referenceDate = ResolveReferenceDate(referenceDate);
        return AnalyzeSimulationCore(input, referenceDate, months);
    }

    public DateOnly GetSuggestedFirstDueDate(Guid cardId, DateOnly purchaseDate)
    {
        var card = cards.FirstOrDefault(item => item.Id == cardId);
        return card is null ? purchaseDate : CalculateSuggestedFirstDueDate(card, purchaseDate);
    }

    public void AddIncome(IncomeEntryInput input)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        db.IncomeEntries.Add(new IncomeEntryEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            Description = input.Description.Trim(),
            AmountCents = ToCents(input.Amount),
            Date = input.Date,
            Recurrence = input.Recurrence,
            Status = input.Status,
            ResponsibleId = input.ResponsibleId,
            CategoryId = input.CategoryId,
            IsExtra = input.IsExtra
        });
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void AddFixedExpense(FixedExpenseInput input)
    {
        if (!HasFamilySetup ||
            members.All(member => member.Id != input.ResponsibleId) ||
            categories.All(category => category.Id != input.CategoryId))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        db.FixedExpenses.Add(new FixedExpenseEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            Description = input.Description.Trim(),
            AmountCents = ToCents(input.Amount),
            DueDay = input.DueDay,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            Status = input.Status,
            ResponsibleId = input.ResponsibleId,
            CategoryId = input.CategoryId,
            IsPaused = false,
            Notes = input.Notes.Trim()
        });
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void AddVariableExpense(VariableExpenseInput input)
    {
        if (!HasFamilySetup ||
            members.All(member => member.Id != input.ResponsibleId) ||
            categories.All(category => category.Id != input.CategoryId))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        db.VariableExpenses.Add(new VariableExpenseEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            Description = input.Description.Trim(),
            AmountCents = ToCents(input.Amount),
            Date = input.Date,
            PaymentMethod = input.PaymentMethod,
            Status = input.Status,
            ResponsibleId = input.ResponsibleId,
            CategoryId = input.CategoryId,
            Notes = input.Notes.Trim()
        });
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void UpdateFixedExpense(FixedExpenseEditInput input)
    {
        if (!HasFamilySetup ||
            members.All(member => member.Id != input.ResponsibleId) ||
            categories.All(category => category.Id != input.CategoryId))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.FixedExpenses.FirstOrDefault(item => item.Id == input.Id && item.FamilyGroupId == family.Id);
        if (entity is null)
        {
            return;
        }

        entity.Description = input.Description.Trim();
        entity.AmountCents = ToCents(input.Amount);
        entity.DueDay = input.DueDay;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.Status = input.Status;
        entity.ResponsibleId = input.ResponsibleId;
        entity.CategoryId = input.CategoryId;
        entity.Notes = input.Notes.Trim();
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void PauseFixedExpense(Guid expenseId)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.FixedExpenses.FirstOrDefault(item => item.Id == expenseId && item.FamilyGroupId == family.Id);
        if (entity is null)
        {
            return;
        }

        entity.IsPaused = !entity.IsPaused;
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void RemoveFixedExpense(Guid expenseId)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.FixedExpenses.FirstOrDefault(item => item.Id == expenseId && item.FamilyGroupId == family.Id);
        if (entity is null)
        {
            return;
        }

        db.FixedExpenses.Remove(entity);
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void AddCardPurchase(CardPurchaseInput input)
    {
        if (!HasFamilySetup ||
            cards.All(card => card.Id != input.CardId) ||
            members.All(member => member.Id != input.ResponsibleId) ||
            categories.All(category => category.Id != input.CategoryId))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        db.CardPurchases.Add(new CardPurchaseEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            CardId = input.CardId,
            Description = input.Description.Trim(),
            TotalAmountCents = ToCents(input.TotalAmount),
            Installments = input.Installments,
            PurchaseDate = input.PurchaseDate,
            FirstDueDate = input.FirstDueDate,
            CategoryId = input.CategoryId,
            ResponsibleId = input.ResponsibleId,
            Notes = input.Notes.Trim(),
            Status = input.Status
        });
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void UpdateCardPurchase(CardPurchaseEditInput input)
    {
        if (!HasFamilySetup ||
            cards.All(card => card.Id != input.CardId) ||
            members.All(member => member.Id != input.ResponsibleId) ||
            categories.All(category => category.Id != input.CategoryId))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.CardPurchases.FirstOrDefault(item => item.Id == input.Id && item.FamilyGroupId == family.Id);
        if (entity is null)
        {
            return;
        }

        entity.CardId = input.CardId;
        entity.Description = input.Description.Trim();
        entity.TotalAmountCents = ToCents(input.TotalAmount);
        entity.Installments = input.Installments;
        entity.PurchaseDate = input.PurchaseDate;
        entity.FirstDueDate = input.FirstDueDate;
        entity.CategoryId = input.CategoryId;
        entity.ResponsibleId = input.ResponsibleId;
        entity.Notes = input.Notes.Trim();
        entity.Status = input.Status;
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void RemoveCardPurchase(Guid purchaseId)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.CardPurchases.FirstOrDefault(item => item.Id == purchaseId && item.FamilyGroupId == family.Id);
        if (entity is null)
        {
            return;
        }

        db.CardPurchases.Remove(entity);
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void SaveSimulation(PurchaseSimulationInput input)
    {
        if (!HasFamilySetup || cards.All(card => card.Id != input.CardId))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        db.PurchaseSimulations.Add(new PurchaseSimulationEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            Description = input.Description.Trim(),
            TotalAmountCents = ToCents(input.TotalAmount),
            IsInstallment = input.IsInstallment,
            Installments = input.IsInstallment ? input.Installments : 1,
            CardId = input.CardId,
            PlannedDate = input.PlannedDate,
            CategoryId = input.CategoryId,
            ResponsibleId = input.ResponsibleId,
            Notes = input.Notes.Trim()
        });
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void RemoveSimulation(Guid simulationId)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.PurchaseSimulations.FirstOrDefault(item => item.Id == simulationId && item.FamilyGroupId == family.Id);
        if (entity is null)
        {
            return;
        }

        db.PurchaseSimulations.Remove(entity);
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void ConvertSimulationToPurchase(Guid simulationId)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var simulation = db.PurchaseSimulations.FirstOrDefault(item => item.Id == simulationId && item.FamilyGroupId == family.Id);
        if (simulation is null)
        {
            return;
        }

        var card = cards.FirstOrDefault(item => item.Id == simulation.CardId);
        if (card is null)
        {
            return;
        }

        db.CardPurchases.Add(new CardPurchaseEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            CardId = simulation.CardId,
            Description = simulation.Description,
            TotalAmountCents = simulation.TotalAmountCents,
            Installments = simulation.IsInstallment ? simulation.Installments : 1,
            PurchaseDate = simulation.PlannedDate,
            FirstDueDate = CalculateSuggestedFirstDueDate(card, simulation.PlannedDate),
            CategoryId = simulation.CategoryId,
            ResponsibleId = simulation.ResponsibleId,
            Notes = simulation.Notes,
            Status = EntryStatus.Planned
        });

        db.PurchaseSimulations.Remove(simulation);
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    private void ReloadState()
    {
        if (currentUser is null)
        {
            ResetState();
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var userEntity = db.AppUsers.AsNoTracking().FirstOrDefault(item => item.Id == currentUser.Id);
        if (userEntity is null)
        {
            currentUser = null;
            ResetState();
            return;
        }

        currentUser = MapUser(userEntity);

        if (userEntity.FamilyGroupId is not Guid familyId)
        {
            ResetState(keepUser: true);
            return;
        }

        var familyEntity = db.FamilyGroups.AsNoTracking().FirstOrDefault(item => item.Id == familyId);
        if (familyEntity is null)
        {
            ResetState(keepUser: true);
            return;
        }

        family = MapFamily(familyEntity);

        members = db.FamilyMembers.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => item.Name)
            .Select(MapMember)
            .ToList();

        categories = db.Categories.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .OrderBy(item => item.Kind)
            .ThenBy(item => item.Name)
            .Select(MapCategory)
            .ToList();

        incomes = db.IncomeEntries.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .Select(MapIncome)
            .ToList();

        fixedExpenses = db.FixedExpenses.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .Select(MapFixedExpense)
            .ToList();

        variableExpenses = db.VariableExpenses.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .Select(MapVariableExpense)
            .ToList();

        cards = db.CreditCards.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .OrderBy(item => item.Name)
            .Select(MapCard)
            .ToList();

        purchases = db.CardPurchases.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .Select(MapPurchase)
            .ToList();

        simulations = db.PurchaseSimulations.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .Select(MapSimulation)
            .ToList();

        allocations = db.SurplusAllocations.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .OrderByDescending(item => item.Month)
            .Select(MapAllocation)
            .ToList();

        goals = db.FinancialGoals.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .OrderBy(item => item.TargetDate)
            .ThenBy(item => item.Name)
            .Select(MapGoal)
            .ToList();

        goalContributions = db.GoalContributions.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .OrderByDescending(item => item.Date)
            .Select(MapGoalContribution)
            .ToList();

        fixedExpenseMonths = db.FixedExpenseMonths.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .Select(MapFixedExpenseMonth)
            .ToList();

        cardBillPayments = db.CardBillPayments.AsNoTracking()
            .Where(item => item.FamilyGroupId == familyId)
            .Select(MapCardBillPayment)
            .ToList();

        if (members.Count == 0)
        {
            SelectedMemberId = Guid.Empty;
            ViewMode = FamilyViewMode.Family;
            return;
        }

        if (currentUser.FamilyMemberId is Guid familyMemberId && members.Any(member => member.Id == familyMemberId))
        {
            SelectedMemberId = familyMemberId;
        }
        else if (members.All(member => member.Id != SelectedMemberId))
        {
            SelectedMemberId = members.First().Id;
        }
    }

    private void ResetState(bool keepUser = false)
    {
        family = EmptyFamily;
        members = [];
        categories = [];
        incomes = [];
        fixedExpenses = [];
        variableExpenses = [];
        cards = [];
        purchases = [];
        simulations = [];
        allocations = [];
        goals = [];
        goalContributions = [];
        fixedExpenseMonths = [];
        cardBillPayments = [];
        SelectedMemberId = Guid.Empty;
        ViewMode = FamilyViewMode.Family;

        if (!keepUser)
        {
            currentUser = null;
        }
    }

    private SimulationInsight AnalyzeSimulationCore(PurchaseSimulationInput input, DateOnly referenceDate, int months)
    {
        var projection = GetProjection(referenceDate, months);
        var firstDueDate = CalculateSuggestedFirstDueDate(GetCard(input.CardId), input.PlannedDate);
        var monthlyAmounts = BuildSimulationSchedule(input, firstDueDate);
        var expenseGoal = GetScopeExpenseGoal();

        var impacts = projection.Months
            .Select(month =>
            {
                var added = monthlyAmounts.TryGetValue(StartOfMonth(month.Month), out var value) ? value : 0m;
                var projectedExpense = month.TotalExpenses + added;
                var projectedSurplus = month.Income - projectedExpense;

                return new SimulationMonthImpact(
                    month.Month,
                    month.Label,
                    added,
                    projectedExpense,
                    projectedSurplus,
                    projectedExpense > expenseGoal);
            })
            .ToList();

        return new SimulationInsight(
            impacts.FirstOrDefault(item => item.Month == StartOfMonth(referenceDate))?.AddedExpense ?? 0m,
            impacts.Count == 0 ? 0m : impacts.Max(item => item.AddedExpense),
            impacts.Any(item => item.AboveGoal),
            firstDueDate,
            impacts);
    }

    private Dictionary<DateOnly, decimal> BuildSimulationSchedule(PurchaseSimulationInput input, DateOnly firstDueDate)
    {
        var schedule = new Dictionary<DateOnly, decimal>();
        var installments = input.IsInstallment ? input.Installments : 1;

        for (var installment = 1; installment <= installments; installment++)
        {
            var month = StartOfMonth(firstDueDate.AddMonths(installment - 1));
            schedule[month] = GetInstallmentAmount(input.TotalAmount, installments, installment);
        }

        return schedule;
    }

    private IReadOnlyList<UpcomingDueItem> GetUpcomingDueItems(DateOnly referenceDate)
    {
        var fixedItems = GetFixedExpenseOccurrences(StartOfMonth(referenceDate))
            .Where(item => !item.IsPaid)
            .Where(item => item.DueDate >= referenceDate && item.DueDate <= referenceDate.AddDays(45))
            .Select(item => new UpcomingDueItem(item.Description, "Despesa fixa", item.Amount, item.DueDate, item.ResponsibleName));

        var variableItems = GetVariableExpenseOccurrences(StartOfMonth(referenceDate))
            .Where(item => item.DueDate >= referenceDate && item.DueDate <= referenceDate.AddDays(45))
            .Select(item => new UpcomingDueItem(item.Description, GetImmediateExpenseSourceLabel(item.CardName), item.Amount, item.DueDate, item.ResponsibleName));

        var futureCardItems = GetCardPurchaseOccurrences(StartOfMonth(referenceDate))
            .Where(item => !item.IsPaid)
            .Where(item => item.DueDate >= referenceDate && item.DueDate <= referenceDate.AddDays(45))
            .Select(item => new UpcomingDueItem(item.Description, $"Cartao {item.CardName}", item.Amount, item.DueDate, item.ResponsibleName));

        var nextMonthFixedItems = GetFixedExpenseOccurrences(StartOfMonth(referenceDate.AddMonths(1)))
            .Where(item => !item.IsPaid)
            .Where(item => item.DueDate >= referenceDate && item.DueDate <= referenceDate.AddDays(45))
            .Select(item => new UpcomingDueItem(item.Description, "Despesa fixa", item.Amount, item.DueDate, item.ResponsibleName));

        var nextMonthVariableItems = GetVariableExpenseOccurrences(StartOfMonth(referenceDate.AddMonths(1)))
            .Where(item => item.DueDate >= referenceDate && item.DueDate <= referenceDate.AddDays(45))
            .Select(item => new UpcomingDueItem(item.Description, GetImmediateExpenseSourceLabel(item.CardName), item.Amount, item.DueDate, item.ResponsibleName));

        var nextMonthCardItems = GetCardPurchaseOccurrences(StartOfMonth(referenceDate.AddMonths(1)))
            .Where(item => !item.IsPaid)
            .Where(item => item.DueDate >= referenceDate && item.DueDate <= referenceDate.AddDays(45))
            .Select(item => new UpcomingDueItem(item.Description, $"Cartao {item.CardName}", item.Amount, item.DueDate, item.ResponsibleName));

        return fixedItems
            .Concat(variableItems)
            .Concat(futureCardItems)
            .Concat(nextMonthFixedItems)
            .Concat(nextMonthVariableItems)
            .Concat(nextMonthCardItems)
            .OrderBy(item => item.DueDate)
            .DistinctBy(item => new { item.Description, item.DueDate, item.Amount, item.ResponsibleName })
            .Take(6)
            .ToList();
    }

    private IReadOnlyList<AlertItem> GetAlerts(DateOnly referenceDate, decimal currentExpenses, decimal expenseGoal)
    {
        var alerts = new List<AlertItem>();
        var dueSoon = GetUpcomingDueItems(referenceDate)
            .Where(item => item.DueDate <= referenceDate.AddDays(7))
            .ToList();

        if (dueSoon.Count > 0)
        {
            alerts.Add(new AlertItem(
                "Contas vencendo",
                $"{dueSoon.Count} compromisso(s) vencem nos proximos 7 dias.",
                "warning"));
        }

        if (expenseGoal > 0 && currentExpenses > expenseGoal)
        {
            alerts.Add(new AlertItem(
                "Meta estourada",
                $"O comprometimento atual do mes esta {(currentExpenses - expenseGoal):C2} acima da meta.",
                "danger"));
        }

        var endingSoon = GetInstallmentOverview(referenceDate).Where(item => item.EndsSoon).Take(2).ToList();
        if (endingSoon.Count > 0)
        {
            alerts.Add(new AlertItem(
                "Parcelas acabando",
                string.Join(" | ", endingSoon.Select(item => $"{item.Description} termina em {item.FinalDueDate:MM/yyyy}")),
                "info"));
        }

        var highImpactSimulation = GetSimulationSummaries(referenceDate)
            .OrderByDescending(item => item.PeakMonthlyImpact)
            .FirstOrDefault(item => item.BreaksGoal);

        if (highImpactSimulation is not null)
        {
            alerts.Add(new AlertItem(
                "Simulacao com impacto alto",
                $"{highImpactSimulation.Description} pressiona o orcamento em mais de um mes.",
                "warning"));
        }

        return alerts;
    }

    private IReadOnlyList<MemberMonthlySummary> GetMemberSummaries(DateOnly month) =>
        members
            .Where(member => ViewMode == FamilyViewMode.Family || member.Id == SelectedMemberId)
            .Select(member =>
            {
                var income = GetMonthlyIncome(month, member.Id);
                var fixedTotal = GetFixedExpenseOccurrences(month, member.Id).Sum(item => item.Amount);
                var variableTotal = GetVariableExpenseOccurrences(month, member.Id).Sum(item => item.Amount);
                var cardTotal = GetCardPurchaseOccurrences(month, member.Id).Sum(item => item.Amount);
                var totalExpense = fixedTotal + variableTotal + cardTotal;

                return new MemberMonthlySummary(member.Name, income, totalExpense, income - totalExpense);
            })
            .ToList();

    private decimal GetScopeExpenseGoal() =>
        CurrentScope.MemberId is Guid memberId
            ? GetMember(memberId).ExpenseGoal
            : family.MonthlyExpenseGoal;

    private DateOnly ResolveReferenceDate(DateOnly referenceDate)
    {
        var requestedMonth = StartOfMonth(referenceDate);
        var firstRelevantMonth = GetFirstRelevantMonth();
        return firstRelevantMonth is not null && requestedMonth < firstRelevantMonth.Value
            ? firstRelevantMonth.Value
            : requestedMonth;
    }

    private DateOnly? GetFirstRelevantMonth()
    {
        var months = FilterScope(incomes.Where(entry => entry.Status != EntryStatus.Cancelled), entry => entry.ResponsibleId)
            .Select(entry => StartOfMonth(entry.Date))
            .Concat(FilterScope(fixedExpenses.Where(entry => entry.Status != EntryStatus.Cancelled && !entry.IsPaused), entry => entry.ResponsibleId)
                .Select(entry => StartOfMonth(entry.StartDate)))
            .Concat(FilterScope(variableExpenses.Where(entry => entry.Status != EntryStatus.Cancelled), entry => entry.ResponsibleId)
                .Select(entry => StartOfMonth(entry.Date)))
            .Concat(FilterScope(simulations, entry => entry.ResponsibleId)
                .Select(entry => StartOfMonth(entry.PlannedDate)))
            .Concat(FilterScope(allocations, entry => entry.ResponsibleId)
                .Select(entry => StartOfMonth(entry.Month)))
            .ToList();

        return months.Count == 0 ? null : months.Min();
    }

    private decimal GetMonthlyIncome(DateOnly month, Guid? memberId = null) =>
        incomes
            .Where(entry => entry.Status != EntryStatus.Cancelled)
            .Where(entry => memberId is null || entry.ResponsibleId == memberId)
            .Where(entry => IsIncomeActiveInMonth(entry, month))
            .Sum(entry => entry.Amount);

    private IEnumerable<ExpenseOccurrence> GetFixedExpenseOccurrences(DateOnly month, Guid? memberId = null) =>
        fixedExpenses
            .Where(entry => entry.Status != EntryStatus.Cancelled && !entry.IsPaused)
            .Where(entry => memberId is null || entry.ResponsibleId == memberId)
            .Where(entry => IsExpenseActiveInMonth(entry, month))
            .Where(entry => IsInScope(entry.ResponsibleId))
            .Select(entry =>
            {
                var monthStart = StartOfMonth(month);
                var record = fixedExpenseMonths.FirstOrDefault(item => item.FixedExpenseId == entry.Id && item.Month == monthStart);

                return new ExpenseOccurrence(
                    entry.Description,
                    SafeMemberName(entry.ResponsibleId),
                    CreateSafeDate(month.Year, month.Month, entry.DueDay),
                    record?.ActualAmount ?? entry.Amount,
                    string.Empty,
                    Guid.Empty,
                    record?.IsPaid ?? false);
            });

    private IEnumerable<ExpenseOccurrence> GetVariableExpenseOccurrences(DateOnly month, Guid? memberId = null) =>
        variableExpenses
            .Where(entry => entry.Status != EntryStatus.Cancelled)
            .Where(entry => memberId is null || entry.ResponsibleId == memberId)
            .Where(entry => IsInScope(entry.ResponsibleId))
            .Where(entry => StartOfMonth(entry.Date) == StartOfMonth(month))
            .Select(entry => new ExpenseOccurrence(
                entry.Description,
                SafeMemberName(entry.ResponsibleId),
                entry.Date,
                entry.Amount,
                entry.PaymentMethod.ToString(),
                Guid.Empty,
                true));

    private IEnumerable<ExpenseOccurrence> GetCardPurchaseOccurrences(DateOnly month, Guid? memberId = null)
    {
        foreach (var purchase in purchases.Where(item => item.Status != EntryStatus.Cancelled))
        {
            if (memberId is not null && purchase.ResponsibleId != memberId)
            {
                continue;
            }

            if (!IsInScope(purchase.ResponsibleId))
            {
                continue;
            }

            var card = GetCard(purchase.CardId);
            for (var installment = 1; installment <= purchase.Installments; installment++)
            {
                var dueDate = purchase.FirstDueDate.AddMonths(installment - 1);
                if (StartOfMonth(dueDate) != StartOfMonth(month))
                {
                    continue;
                }

                yield return new ExpenseOccurrence(
                    purchase.Description,
                    SafeMemberName(purchase.ResponsibleId),
                    dueDate,
                    GetInstallmentAmount(purchase, installment),
                    card.Name,
                    card.Id,
                    cardBillPayments.Any(item => item.CardId == card.Id && item.Month == StartOfMonth(month) && item.IsPaid));
            }
        }
    }

    private decimal GetReleasedAmount(DateOnly month) =>
        FilterScope(purchases.Where(item => item.Status != EntryStatus.Cancelled), entry => entry.ResponsibleId)
            .Where(purchase => StartOfMonth(GetFinalDueDate(purchase)) == month)
            .Sum(purchase => GetInstallmentAmount(purchase, 1));

    private int GetCurrentInstallmentNumber(CardPurchase purchase, DateOnly referenceDate)
    {
        var currentMonth = StartOfMonth(referenceDate);
        var firstMonth = StartOfMonth(purchase.FirstDueDate);
        var diff = ((currentMonth.Year - firstMonth.Year) * 12) + currentMonth.Month - firstMonth.Month + 1;
        return Math.Clamp(diff, 1, purchase.Installments);
    }

    private int GetRemainingInstallments(CardPurchase purchase, DateOnly referenceDate) =>
        Enumerable.Range(1, purchase.Installments)
            .Select(index => purchase.FirstDueDate.AddMonths(index - 1))
            .Count(dueDate => StartOfMonth(dueDate) > StartOfMonth(referenceDate));

    private DateOnly GetFinalDueDate(CardPurchase purchase) =>
        purchase.FirstDueDate.AddMonths(purchase.Installments - 1);

    private decimal GetInstallmentAmount(CardPurchase purchase, int installmentNumber) =>
        GetInstallmentAmount(purchase.TotalAmount, purchase.Installments, installmentNumber);

    private static decimal GetInstallmentAmount(decimal totalAmount, int installments, int installmentNumber)
    {
        var regularAmount = decimal.Round(totalAmount / installments, 2, MidpointRounding.AwayFromZero);
        return installmentNumber < installments
            ? regularAmount
            : totalAmount - (regularAmount * (installments - 1));
    }

    private static bool IsIncomeActiveInMonth(IncomeEntry entry, DateOnly month)
    {
        var monthStart = StartOfMonth(month);
        return entry.Recurrence == RecurrenceType.Monthly
            ? StartOfMonth(entry.Date) <= monthStart
            : StartOfMonth(entry.Date) == monthStart;
    }

    private static bool IsExpenseActiveInMonth(FixedExpense expense, DateOnly month)
    {
        var monthStart = StartOfMonth(month);
        var start = StartOfMonth(expense.StartDate);
        DateOnly? end = expense.EndDate is null ? null : StartOfMonth(expense.EndDate.Value);
        return start <= monthStart && (end is null || end >= monthStart);
    }

    private bool IsInScope(Guid responsibleId) =>
        ViewMode == FamilyViewMode.Family || responsibleId == SelectedMemberId;

    private IEnumerable<T> FilterScope<T>(IEnumerable<T> entries, Func<T, Guid> responsibleSelector) =>
        entries.Where(entry => ViewMode == FamilyViewMode.Family || responsibleSelector(entry) == SelectedMemberId);

    private FamilyMember GetMember(Guid memberId) =>
        members.First(member => member.Id == memberId);

    private Category GetCategory(Guid categoryId) =>
        categories.First(category => category.Id == categoryId);

    private CreditCard GetCard(Guid cardId) =>
        cards.First(card => card.Id == cardId);

    private string SafeMemberName(Guid memberId) =>
        members.FirstOrDefault(item => item.Id == memberId)?.Name ?? "Responsavel";

    private string SafeCardName(Guid cardId) =>
        cards.FirstOrDefault(item => item.Id == cardId)?.Name ?? "Cartao";

    private static string GetImmediateExpenseSourceLabel(string source) =>
        source switch
        {
            nameof(ExpensePaymentMethod.Pix) => "Pix",
            nameof(ExpensePaymentMethod.Cash) => "Dinheiro",
            _ => source
        };

    private DateOnly CalculateSuggestedFirstDueDate(CreditCard card, DateOnly purchaseDate)
    {
        var currentMonthDueDate = CreateSafeDate(purchaseDate.Year, purchaseDate.Month, card.DueDay);
        if (purchaseDate <= currentMonthDueDate)
        {
            return currentMonthDueDate;
        }

        var nextMonth = purchaseDate.AddMonths(1);
        return CreateSafeDate(nextMonth.Year, nextMonth.Month, card.DueDay);
    }

    private static FamilyGroup MapFamily(FamilyGroupEntity entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.AccessCode,
            FromCents(entity.MonthlyExpenseGoalCents),
            FromCents(entity.MonthlyInvestmentGoalCents),
            FromCents(entity.CheckingBalanceCents),
            FromCents(entity.InvestedBalanceCents));

    private static AppUserProfile MapUser(AppUserEntity entity) =>
        new(
            entity.Id,
            entity.DisplayName,
            entity.Email,
            entity.FamilyGroupId,
            entity.FamilyMemberId,
            entity.Role);

    private static FamilyMember MapMember(FamilyMemberEntity entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Email,
            FromCents(entity.MonthlyIncomeCents),
            FromCents(entity.ExpenseGoalCents),
            FromCents(entity.InvestmentGoalCents));

    private static Category MapCategory(CategoryEntity entity) =>
        new(entity.Id, entity.Name, entity.Kind, entity.Color);

    private static IncomeEntry MapIncome(IncomeEntryEntity entity) =>
        new(
            entity.Id,
            entity.Description,
            FromCents(entity.AmountCents),
            entity.Date,
            entity.Recurrence,
            entity.Status,
            entity.ResponsibleId,
            entity.CategoryId,
            entity.IsExtra);

    private static FixedExpense MapFixedExpense(FixedExpenseEntity entity) =>
        new(
            entity.Id,
            entity.Description,
            FromCents(entity.AmountCents),
            entity.DueDay,
            entity.StartDate,
            entity.EndDate,
            entity.Status,
            entity.ResponsibleId,
            entity.CategoryId,
            entity.IsPaused,
            entity.Notes);

    private static VariableExpense MapVariableExpense(VariableExpenseEntity entity) =>
        new(
            entity.Id,
            entity.Description,
            FromCents(entity.AmountCents),
            entity.Date,
            entity.PaymentMethod,
            entity.ResponsibleId,
            entity.CategoryId,
            entity.Notes,
            entity.Status);

    private static CreditCard MapCard(CreditCardEntity entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Issuer,
            entity.HolderId,
            entity.DueDay,
            entity.LimitCents is null ? null : FromCents(entity.LimitCents.Value));

    private static CardPurchase MapPurchase(CardPurchaseEntity entity) =>
        new(
            entity.Id,
            entity.CardId,
            entity.Description,
            FromCents(entity.TotalAmountCents),
            entity.Installments,
            entity.PurchaseDate,
            entity.FirstDueDate,
            entity.CategoryId,
            entity.ResponsibleId,
            entity.Notes,
            entity.Status);

    private static PurchaseSimulation MapSimulation(PurchaseSimulationEntity entity) =>
        new(
            entity.Id,
            entity.Description,
            FromCents(entity.TotalAmountCents),
            entity.IsInstallment,
            entity.Installments,
            entity.CardId,
            entity.PlannedDate,
            entity.CategoryId,
            entity.ResponsibleId,
            entity.Notes);

    private static SurplusAllocation MapAllocation(SurplusAllocationEntity entity) =>
        new(
            entity.Id,
            entity.Month,
            FromCents(entity.AmountCents),
            entity.Destination,
            entity.ResponsibleId,
            entity.Notes);

    private static long ToCents(decimal value) =>
        decimal.ToInt64(decimal.Round(value * 100m, 0, MidpointRounding.AwayFromZero));

    private static decimal FromCents(long value) => value / 100m;

    private static DateOnly StartOfMonth(DateOnly value) => new(value.Year, value.Month, 1);

    private static DateOnly CreateSafeDate(int year, int month, int day)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, lastDay));
    }

    private static SimulationInsight EmptySimulation(DateOnly date) =>
        new(0m, 0m, false, date, []);

    private void NotifyStateChanged() => StateChanged?.Invoke();

    private sealed record ExpenseOccurrence(
        string Description,
        string ResponsibleName,
        DateOnly DueDate,
        decimal Amount,
        string CardName,
        Guid CardId,
        bool IsPaid);
}
