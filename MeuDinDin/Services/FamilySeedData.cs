using MeuDinDin.Data;

namespace MeuDinDin.Services;

public static class FamilySeedData
{
    public static IEnumerable<CategoryEntity> BuildDefaultCategories(Guid familyId) =>
    [
        CreateCategory(familyId, "Salario", CategoryKind.Revenue, "#1C78EB", true),
        CreateCategory(familyId, "Freelance", CategoryKind.Revenue, "#0FB5A8", true),
        CreateCategory(familyId, "Mercado", CategoryKind.Expense, "#4E8DF0", true),
        CreateCategory(familyId, "Combustivel", CategoryKind.Expense, "#4FA2D8", true),
        CreateCategory(familyId, "Energia", CategoryKind.Expense, "#0FB5A8", true),
        CreateCategory(familyId, "Internet", CategoryKind.Expense, "#2577DB", true),
        CreateCategory(familyId, "Financiamento", CategoryKind.Expense, "#17173B", true),
        CreateCategory(familyId, "Faculdade", CategoryKind.Expense, "#5E87FF", true),
        CreateCategory(familyId, "Cartao", CategoryKind.Expense, "#3E69C1", true),
        CreateCategory(familyId, "Assinatura", CategoryKind.Expense, "#7A8CCB", true),
        CreateCategory(familyId, "Viagem", CategoryKind.Expense, "#0FB5A8", true),
        CreateCategory(familyId, "Farmacia", CategoryKind.Expense, "#6AA7F2", true),
        CreateCategory(familyId, "Compras online", CategoryKind.Expense, "#2A92E8", true),
        CreateCategory(familyId, "Investimento", CategoryKind.Expense, "#0D9E93", true),
        CreateCategory(familyId, "Lazer", CategoryKind.Expense, "#4C7EF0", true),
        CreateCategory(familyId, "Alimentacao", CategoryKind.Expense, "#377DE0", true)
    ];

    private static CategoryEntity CreateCategory(Guid familyId, string name, CategoryKind kind, string color, bool isSystem) =>
        new()
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = familyId,
            Name = name,
            Kind = kind,
            Color = color,
            IsSystem = isSystem
        };
}
