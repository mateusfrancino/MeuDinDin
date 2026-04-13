using Microsoft.EntityFrameworkCore;

namespace MeuDinDin.Data;

public static class AppDbSchema
{
    public static void EnsureLatest(AppDbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "financial_goals" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_financial_goals" PRIMARY KEY,
                "FamilyGroupId" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "TargetAmountCents" INTEGER NOT NULL,
                "TargetDate" TEXT NULL,
                "ResponsibleId" TEXT NOT NULL,
                "Notes" TEXT NOT NULL DEFAULT '',
                "CreatedUtc" TEXT NOT NULL
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "goal_contributions" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_goal_contributions" PRIMARY KEY,
                "FamilyGroupId" TEXT NOT NULL,
                "GoalId" TEXT NOT NULL,
                "AmountCents" INTEGER NOT NULL,
                "Date" TEXT NOT NULL,
                "ResponsibleId" TEXT NOT NULL,
                "Notes" TEXT NOT NULL DEFAULT ''
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_financial_goals_FamilyGroupId"
            ON "financial_goals" ("FamilyGroupId");
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_goal_contributions_GoalId"
            ON "goal_contributions" ("GoalId");
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "fixed_expense_months" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_fixed_expense_months" PRIMARY KEY,
                "FamilyGroupId" TEXT NOT NULL,
                "FixedExpenseId" TEXT NOT NULL,
                "Month" TEXT NOT NULL,
                "ActualAmountCents" INTEGER NULL,
                "IsPaid" INTEGER NOT NULL DEFAULT 0,
                "PaidDate" TEXT NULL
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "variable_expenses" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_variable_expenses" PRIMARY KEY,
                "FamilyGroupId" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "AmountCents" INTEGER NOT NULL,
                "Date" TEXT NOT NULL,
                "PaymentMethod" INTEGER NOT NULL,
                "Status" INTEGER NOT NULL,
                "ResponsibleId" TEXT NOT NULL,
                "CategoryId" TEXT NOT NULL,
                "Notes" TEXT NOT NULL DEFAULT ''
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "card_bill_payments" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_card_bill_payments" PRIMARY KEY,
                "FamilyGroupId" TEXT NOT NULL,
                "CardId" TEXT NOT NULL,
                "Month" TEXT NOT NULL,
                "IsPaid" INTEGER NOT NULL DEFAULT 0,
                "PaidDate" TEXT NULL
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_fixed_expense_months_FixedExpenseId_Month"
            ON "fixed_expense_months" ("FixedExpenseId", "Month");
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_variable_expenses_Date"
            ON "variable_expenses" ("Date");
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_card_bill_payments_CardId_Month"
            ON "card_bill_payments" ("CardId", "Month");
            """);
    }
}
