using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace MeuDinDin.Data;

public static class AppDbSchema
{
    public static void EnsureLatest(AppDbContext dbContext)
    {
        if (!string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            return;
        }

        EnsureColumnExists(dbContext, "family_groups", "AccessCode", "\"AccessCode\" TEXT NOT NULL DEFAULT ''");

        dbContext.Database.ExecuteSqlRaw(
            """
            UPDATE "family_groups"
            SET "AccessCode" = UPPER(SUBSTR(REPLACE("Id", '-', ''), 1, 8))
            WHERE TRIM(COALESCE("AccessCode", '')) = '';
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_family_groups_AccessCode"
            ON "family_groups" ("AccessCode");
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "app_users" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_app_users" PRIMARY KEY,
                "Email" TEXT NOT NULL,
                "NormalizedEmail" TEXT NOT NULL,
                "DisplayName" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "FamilyGroupId" TEXT NULL,
                "FamilyMemberId" TEXT NULL,
                "Role" TEXT NOT NULL DEFAULT 'Member',
                "CreatedUtc" TEXT NOT NULL,
                "LastLoginUtc" TEXT NULL
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_app_users_NormalizedEmail"
            ON "app_users" ("NormalizedEmail");
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_app_users_FamilyGroupId"
            ON "app_users" ("FamilyGroupId");
            """);

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

    private static void EnsureColumnExists(AppDbContext dbContext, string tableName, string columnName, string columnDefinition)
    {
        if (HasColumn(dbContext, tableName, columnName))
        {
            return;
        }

        ExecuteNonQuery(dbContext, $"""ALTER TABLE "{tableName}" ADD COLUMN {columnDefinition};""");
    }

    private static bool HasColumn(AppDbContext dbContext, string tableName, string columnName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    private static void ExecuteNonQuery(AppDbContext dbContext, string sql)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }
}
