namespace MeuDinDin.Services;

public sealed partial class FinanceHub
{
    public CardDetailSnapshot? GetCardDetails(Guid cardId, DateOnly referenceDate)
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

        referenceDate = ResolveReferenceDate(referenceDate);
        var currentMonth = StartOfMonth(referenceDate);
        var previousMonth = StartOfMonth(currentMonth.AddMonths(-1));
        var nextMonth = StartOfMonth(currentMonth.AddMonths(1));

        var cardPurchases = FilterScope(
                purchases.Where(item => item.Status != EntryStatus.Cancelled && item.CardId == cardId),
                item => item.ResponsibleId)
            .ToList();

        var monthBreakdowns = new[]
        {
            BuildCardMonthBreakdown(cardPurchases, previousMonth),
            BuildCardMonthBreakdown(cardPurchases, currentMonth),
            BuildCardMonthBreakdown(cardPurchases, nextMonth)
        };

        var purchaseIds = cardPurchases.Select(item => item.Id).ToHashSet();
        var activeInstallments = GetInstallmentOverview(referenceDate)
            .Where(item => purchaseIds.Contains(item.PurchaseId))
            .ToList();

        var recentlyFinished = cardPurchases
            .Where(item =>
            {
                var finalMonth = StartOfMonth(GetFinalDueDate(item));
                return finalMonth >= previousMonth && finalMonth <= currentMonth;
            })
            .OrderByDescending(item => GetFinalDueDate(item))
            .Select(item => new CardFinishedPurchase(
                item.Id,
                item.Description,
                SafeMemberName(item.ResponsibleId),
                GetCategory(item.CategoryId).Name,
                GetFinalDueDate(item),
                GetInstallmentAmount(item, 1)))
            .ToList();

        return new CardDetailSnapshot(
            card.Id,
            card.Name,
            card.Issuer,
            SafeMemberName(card.HolderId),
            card.DueDay,
            card.Limit,
            monthBreakdowns[0].Total,
            monthBreakdowns[1].Total,
            monthBreakdowns[2].Total,
            activeInstallments.Count,
            recentlyFinished.Count,
            recentlyFinished.Sum(item => item.ReleasedAmount),
            monthBreakdowns,
            recentlyFinished,
            activeInstallments);
    }

    private CardMonthBreakdown BuildCardMonthBreakdown(IReadOnlyList<CardPurchase> cardPurchases, DateOnly month)
    {
        var targetMonth = StartOfMonth(month);
        var charges = cardPurchases
            .SelectMany(purchase => Enumerable.Range(1, purchase.Installments)
                .Select(installmentNumber => new
                {
                    Purchase = purchase,
                    InstallmentNumber = installmentNumber,
                    DueDate = purchase.FirstDueDate.AddMonths(installmentNumber - 1)
                }))
            .Where(item => StartOfMonth(item.DueDate) == targetMonth)
            .OrderBy(item => item.DueDate)
            .ThenBy(item => item.Purchase.Description)
            .Select(item => new CardChargeLine(
                item.Purchase.Id,
                item.Purchase.Description,
                SafeMemberName(item.Purchase.ResponsibleId),
                GetCategory(item.Purchase.CategoryId).Name,
                item.DueDate,
                GetInstallmentAmount(item.Purchase, item.InstallmentNumber),
                $"{item.InstallmentNumber}/{item.Purchase.Installments}",
                item.InstallmentNumber == item.Purchase.Installments))
            .ToList();

        return new CardMonthBreakdown(
            targetMonth,
            targetMonth.ToString("MMM/yy", culture),
            charges.Sum(item => item.Amount),
            charges);
    }
}
