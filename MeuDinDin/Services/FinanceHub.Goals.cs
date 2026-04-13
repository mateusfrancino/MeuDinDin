using Microsoft.EntityFrameworkCore;
using MeuDinDin.Data;

namespace MeuDinDin.Services;

public sealed partial class FinanceHub
{
    private List<FinancialGoal> goals = [];
    private List<GoalContribution> goalContributions = [];

    public IReadOnlyList<GoalProgressSummary> GetGoalProgress(DateOnly referenceDate)
    {
        if (!HasFamilySetup)
        {
            return [];
        }

        referenceDate = ResolveReferenceDate(referenceDate);

        return FilterScope(goals, goal => goal.ResponsibleId)
            .Select(goal => BuildGoalProgress(goal, referenceDate))
            .OrderBy(summary => summary.IsCompleted)
            .ThenBy(summary => summary.TargetDate ?? DateOnly.MaxValue)
            .ThenBy(summary => summary.Name)
            .ToList();
    }

    public void AddGoal(GoalInput input)
    {
        if (!HasFamilySetup || members.All(member => member.Id != input.ResponsibleId))
        {
            return;
        }

        var name = input.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        db.FinancialGoals.Add(new FinancialGoalEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            Name = name,
            TargetAmountCents = ToCents(input.TargetAmount),
            TargetDate = input.TargetDate,
            ResponsibleId = input.ResponsibleId,
            Notes = input.Notes.Trim()
        });
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void UpdateGoal(GoalEditInput input)
    {
        if (!HasFamilySetup || members.All(member => member.Id != input.ResponsibleId))
        {
            return;
        }

        var name = input.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.FinancialGoals.FirstOrDefault(goal => goal.Id == input.Id && goal.FamilyGroupId == family.Id);
        if (entity is null)
        {
            return;
        }

        entity.Name = name;
        entity.TargetAmountCents = ToCents(input.TargetAmount);
        entity.TargetDate = input.TargetDate;
        entity.ResponsibleId = input.ResponsibleId;
        entity.Notes = input.Notes.Trim();
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void RemoveGoal(Guid goalId)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.FinancialGoals.FirstOrDefault(goal => goal.Id == goalId && goal.FamilyGroupId == family.Id);
        if (entity is null)
        {
            return;
        }

        var contributions = db.GoalContributions
            .Where(item => item.GoalId == goalId && item.FamilyGroupId == family.Id)
            .ToList();

        if (contributions.Count > 0)
        {
            db.GoalContributions.RemoveRange(contributions);
        }

        db.FinancialGoals.Remove(entity);
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    public void AddGoalContribution(GoalContributionInput input)
    {
        if (!HasFamilySetup || members.All(member => member.Id != input.ResponsibleId))
        {
            return;
        }

        using var db = dbContextFactory.CreateDbContext();
        var goalExists = db.FinancialGoals.Any(goal => goal.Id == input.GoalId && goal.FamilyGroupId == family.Id);
        if (!goalExists)
        {
            return;
        }

        db.GoalContributions.Add(new GoalContributionEntity
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = family.Id,
            GoalId = input.GoalId,
            AmountCents = ToCents(input.Amount),
            Date = input.Date,
            ResponsibleId = input.ResponsibleId,
            Notes = input.Notes.Trim()
        });
        db.SaveChanges();

        ReloadState();
        NotifyStateChanged();
    }

    private GoalProgressSummary BuildGoalProgress(FinancialGoal goal, DateOnly referenceDate)
    {
        var contributions = goalContributions
            .Where(item => item.GoalId == goal.Id)
            .OrderByDescending(item => item.Date)
            .ThenByDescending(item => item.Id)
            .ToList();

        var savedAmount = contributions.Sum(item => item.Amount);
        var remainingAmount = Math.Max(goal.TargetAmount - savedAmount, 0m);
        var progressRatio = goal.TargetAmount <= 0m ? 0m : Math.Min(savedAmount / goal.TargetAmount, 1m);
        var remainingMonths = CalculateRemainingMonths(referenceDate, goal.TargetDate, remainingAmount);
        var suggestedMonthlyContribution = remainingMonths == 0 ? 0m : remainingAmount / remainingMonths;

        return new GoalProgressSummary(
            goal.Id,
            goal.Name,
            goal.TargetAmount,
            savedAmount,
            remainingAmount,
            progressRatio,
            goal.TargetDate,
            remainingMonths,
            suggestedMonthlyContribution,
            goal.ResponsibleId,
            SafeMemberName(goal.ResponsibleId),
            goal.Notes,
            remainingAmount == 0m && savedAmount > 0m,
            contributions
                .Take(4)
                .Select(item => new GoalContributionItem(
                    item.Id,
                    item.Amount,
                    item.Date,
                    SafeMemberName(item.ResponsibleId),
                    item.Notes))
                .ToList());
    }

    private static int CalculateRemainingMonths(DateOnly referenceDate, DateOnly? targetDate, decimal remainingAmount)
    {
        if (targetDate is null || remainingAmount <= 0m)
        {
            return 0;
        }

        var startMonth = StartOfMonth(referenceDate);
        var targetMonth = StartOfMonth(targetDate.Value);
        var months = ((targetMonth.Year - startMonth.Year) * 12) + targetMonth.Month - startMonth.Month + 1;
        return Math.Max(months, 1);
    }

    private static FinancialGoal MapGoal(FinancialGoalEntity entity) =>
        new(
            entity.Id,
            entity.Name,
            FromCents(entity.TargetAmountCents),
            entity.TargetDate,
            entity.ResponsibleId,
            entity.Notes,
            entity.CreatedUtc);

    private static GoalContribution MapGoalContribution(GoalContributionEntity entity) =>
        new(
            entity.Id,
            entity.GoalId,
            FromCents(entity.AmountCents),
            entity.Date,
            entity.ResponsibleId,
            entity.Notes);
}
