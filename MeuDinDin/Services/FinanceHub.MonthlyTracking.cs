using Microsoft.EntityFrameworkCore;
using MeuDinDin.Data;

namespace MeuDinDin.Services;

public sealed partial class FinanceHub
{
    private List<FixedExpenseMonthRecord> fixedExpenseMonths = [];
    private List<CardBillPaymentRecord> cardBillPayments = [];

    public IReadOnlyList<FixedExpenseMonthStatus> GetFixedExpenseMonthStatuses(DateOnly month)
    {
        if (!HasFamilySetup)
        {
            return [];
        }

        var targetMonth = StartOfMonth(ResolveReferenceDate(month));

        return FilterScope(
                fixedExpenses.Where(expense => expense.Status != EntryStatus.Cancelled && !expense.IsPaused)
                    .Where(expense => IsExpenseActiveInMonth(expense, targetMonth)),
                expense => expense.ResponsibleId)
            .OrderBy(expense => expense.DueDay)
            .ThenBy(expense => expense.Description)
            .Select(expense =>
            {
                var record = fixedExpenseMonths.FirstOrDefault(item => item.FixedExpenseId == expense.Id && item.Month == targetMonth);
                var actualAmount = record?.ActualAmount;
                var effectiveAmount = actualAmount ?? expense.Amount;

                return new FixedExpenseMonthStatus(
                    expense.Id,
                    expense.Description,
                    SafeMemberName(expense.ResponsibleId),
                    GetCategory(expense.CategoryId).Name,
                    targetMonth,
                    CreateSafeDate(targetMonth.Year, targetMonth.Month, expense.DueDay),
                    expense.Amount,
                    actualAmount,
                    effectiveAmount,
                    record?.IsPaid ?? false);
            })
            .ToList();
    }

    public CardBillStatus? GetCardBillStatus(Guid cardId, DateOnly month)
    {
        if (!HasFamilySetup)
        {
            return null;
        }

        var card = cards.FirstOrDefault(item => item.Id == cardId);
        if (card is null)
        {
            return null;
        }

        var targetMonth = StartOfMonth(ResolveReferenceDate(month));
        var amount = GetCardPurchaseOccurrences(targetMonth)
            .Where(item => item.CardId == cardId)
            .Sum(item => item.Amount);

        var record = cardBillPayments.FirstOrDefault(item => item.CardId == cardId && item.Month == targetMonth);
        return new CardBillStatus(
            cardId,
            card.Name,
            targetMonth,
            CreateSafeDate(targetMonth.Year, targetMonth.Month, card.DueDay),
            amount,
            record?.IsPaid ?? false);
    }

    public void UpdateFixedExpenseMonth(FixedExpenseMonthUpdateInput input)
    {
        if (!HasFamilySetup)
        {
            return;
        }

        var expense = fixedExpenses.FirstOrDefault(item => item.Id == input.ExpenseId);
        if (expense is null)
        {
            return;
        }

        var month = StartOfMonth(input.Month);
        var actualAmount = input.ActualAmount is > 0m ? input.ActualAmount : null;

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.FixedExpenseMonths.FirstOrDefault(item =>
            item.FixedExpenseId == input.ExpenseId &&
            item.Month == month &&
            item.FamilyGroupId == family.Id);

        if (entity is null)
        {
            if (actualAmount is null && !input.IsPaid)
            {
                return;
            }

            db.FixedExpenseMonths.Add(new FixedExpenseMonthEntity
            {
                Id = Guid.NewGuid(),
                FamilyGroupId = family.Id,
                FixedExpenseId = input.ExpenseId,
                Month = month,
                ActualAmountCents = actualAmount is null ? null : ToCents(actualAmount.Value),
                IsPaid = input.IsPaid,
                PaidDate = input.IsPaid ? DateOnly.FromDateTime(DateTime.Today) : null
            });
        }
        else if (actualAmount is null && !input.IsPaid)
        {
            db.FixedExpenseMonths.Remove(entity);
        }
        else
        {
            entity.ActualAmountCents = actualAmount is null ? null : ToCents(actualAmount.Value);
            entity.IsPaid = input.IsPaid;
            entity.PaidDate = input.IsPaid ? entity.PaidDate ?? DateOnly.FromDateTime(DateTime.Today) : null;
        }

        db.SaveChanges();
        ReloadState();
        NotifyStateChanged();
    }

    public void SetCardBillPayment(CardBillPaymentInput input)
    {
        if (!HasFamilySetup || cards.All(card => card.Id != input.CardId))
        {
            return;
        }

        var month = StartOfMonth(input.Month);

        using var db = dbContextFactory.CreateDbContext();
        var entity = db.CardBillPayments.FirstOrDefault(item =>
            item.CardId == input.CardId &&
            item.Month == month &&
            item.FamilyGroupId == family.Id);

        if (entity is null)
        {
            if (!input.IsPaid)
            {
                return;
            }

            db.CardBillPayments.Add(new CardBillPaymentEntity
            {
                Id = Guid.NewGuid(),
                FamilyGroupId = family.Id,
                CardId = input.CardId,
                Month = month,
                IsPaid = true,
                PaidDate = DateOnly.FromDateTime(DateTime.Today)
            });
        }
        else if (!input.IsPaid)
        {
            db.CardBillPayments.Remove(entity);
        }
        else
        {
            entity.IsPaid = true;
            entity.PaidDate = entity.PaidDate ?? DateOnly.FromDateTime(DateTime.Today);
        }

        db.SaveChanges();
        ReloadState();
        NotifyStateChanged();
    }

    private static FixedExpenseMonthRecord MapFixedExpenseMonth(FixedExpenseMonthEntity entity) =>
        new(
            entity.FixedExpenseId,
            entity.Month,
            entity.ActualAmountCents is null ? null : FromCents(entity.ActualAmountCents.Value),
            entity.IsPaid,
            entity.PaidDate);

    private static CardBillPaymentRecord MapCardBillPayment(CardBillPaymentEntity entity) =>
        new(
            entity.CardId,
            entity.Month,
            entity.IsPaid,
            entity.PaidDate);

    private sealed record FixedExpenseMonthRecord(
        Guid FixedExpenseId,
        DateOnly Month,
        decimal? ActualAmount,
        bool IsPaid,
        DateOnly? PaidDate);

    private sealed record CardBillPaymentRecord(
        Guid CardId,
        DateOnly Month,
        bool IsPaid,
        DateOnly? PaidDate);
}
