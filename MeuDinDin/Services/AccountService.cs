using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MeuDinDin.Data;

namespace MeuDinDin.Services;

public interface IAccountService
{
    Task<AccountLoginResult> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AccountRegistrationResult> RegisterWithNewFamilyAsync(NewFamilyRegistrationInput input, CancellationToken cancellationToken = default);

    Task<AccountRegistrationResult> RegisterWithExistingFamilyAsync(ExistingFamilyRegistrationInput input, CancellationToken cancellationToken = default);

    Task<AccountActionResult> CreateFamilyForUserAsync(Guid userId, CreateFamilyInput input, CancellationToken cancellationToken = default);

    Task<AccountActionResult> JoinFamilyForUserAsync(Guid userId, JoinFamilyInput input, CancellationToken cancellationToken = default);
}

public sealed class AccountService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IPasswordHasher<AppUserEntity> passwordHasher) : IAccountService
{
    private const string OwnerRole = "Owner";
    private const string MemberRole = "Member";

    public async Task<AccountLoginResult> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
        {
            return AccountLoginResult.Fail("Informe e-mail e senha.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.AppUsers.FirstOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is null)
        {
            return AccountLoginResult.Fail("E-mail ou senha invalidos.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return AccountLoginResult.Fail("E-mail ou senha invalidos.");
        }

        user.LastLoginUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return AccountLoginResult.Success(user);
    }

    public async Task<AccountRegistrationResult> RegisterWithNewFamilyAsync(NewFamilyRegistrationInput input, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(input.Email);
        if (!ValidateCommonRegistration(input.DisplayName, normalizedEmail, input.Password, out var validationError))
        {
            return AccountRegistrationResult.Fail(validationError);
        }

        if (string.IsNullOrWhiteSpace(input.FamilyName))
        {
            return AccountRegistrationResult.Fail("Informe o nome da familia.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await EmailExistsAsync(db, normalizedEmail, cancellationToken))
        {
            return AccountRegistrationResult.Fail("Ja existe uma conta com este e-mail.");
        }

        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var user = CreateUser(input.DisplayName, input.Email, normalizedEmail, input.Password, familyId, memberId, OwnerRole);
        var accessCode = await GenerateUniqueAccessCodeAsync(db, cancellationToken);

        db.FamilyGroups.Add(new FamilyGroupEntity
        {
            Id = familyId,
            Name = input.FamilyName.Trim(),
            AccessCode = accessCode,
            MonthlyExpenseGoalCents = 0,
            MonthlyInvestmentGoalCents = 0,
            CheckingBalanceCents = 0,
            InvestedBalanceCents = 0
        });

        db.FamilyMembers.Add(new FamilyMemberEntity
        {
            Id = memberId,
            FamilyGroupId = familyId,
            Name = input.DisplayName.Trim(),
            Email = input.Email.Trim(),
            MonthlyIncomeCents = 0,
            ExpenseGoalCents = 0,
            InvestmentGoalCents = 0,
            IsPrimary = true
        });

        db.AppUsers.Add(user);
        db.Categories.AddRange(FamilySeedData.BuildDefaultCategories(familyId));
        await db.SaveChangesAsync(cancellationToken);

        return AccountRegistrationResult.Success(user);
    }

    public async Task<AccountRegistrationResult> RegisterWithExistingFamilyAsync(ExistingFamilyRegistrationInput input, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(input.Email);
        if (!ValidateCommonRegistration(input.DisplayName, normalizedEmail, input.Password, out var validationError))
        {
            return AccountRegistrationResult.Fail(validationError);
        }

        var accessCode = NormalizeAccessCode(input.AccessCode);
        if (string.IsNullOrWhiteSpace(accessCode))
        {
            return AccountRegistrationResult.Fail("Informe o codigo da familia.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await EmailExistsAsync(db, normalizedEmail, cancellationToken))
        {
            return AccountRegistrationResult.Fail("Ja existe uma conta com este e-mail.");
        }

        var family = await db.FamilyGroups.FirstOrDefaultAsync(item => item.AccessCode == accessCode, cancellationToken);
        if (family is null)
        {
            return AccountRegistrationResult.Fail("Codigo da familia invalido.");
        }

        var memberId = Guid.NewGuid();
        db.FamilyMembers.Add(new FamilyMemberEntity
        {
            Id = memberId,
            FamilyGroupId = family.Id,
            Name = input.DisplayName.Trim(),
            Email = input.Email.Trim(),
            MonthlyIncomeCents = 0,
            ExpenseGoalCents = 0,
            InvestmentGoalCents = 0,
            IsPrimary = false
        });

        var user = CreateUser(input.DisplayName, input.Email, normalizedEmail, input.Password, family.Id, memberId, MemberRole);
        db.AppUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return AccountRegistrationResult.Success(user);
    }

    public async Task<AccountActionResult> CreateFamilyForUserAsync(Guid userId, CreateFamilyInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.FamilyName))
        {
            return AccountActionResult.Fail("Informe o nome da familia.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.AppUsers.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return AccountActionResult.Fail("Usuario nao encontrado.");
        }

        if (user.FamilyGroupId is not null)
        {
            return AccountActionResult.Fail("Este usuario ja esta vinculado a uma familia.");
        }

        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var accessCode = await GenerateUniqueAccessCodeAsync(db, cancellationToken);

        db.FamilyGroups.Add(new FamilyGroupEntity
        {
            Id = familyId,
            Name = input.FamilyName.Trim(),
            AccessCode = accessCode,
            MonthlyExpenseGoalCents = 0,
            MonthlyInvestmentGoalCents = 0,
            CheckingBalanceCents = 0,
            InvestedBalanceCents = 0
        });

        db.FamilyMembers.Add(new FamilyMemberEntity
        {
            Id = memberId,
            FamilyGroupId = familyId,
            Name = input.MemberName.Trim(),
            Email = user.Email,
            MonthlyIncomeCents = 0,
            ExpenseGoalCents = 0,
            InvestmentGoalCents = 0,
            IsPrimary = true
        });

        user.DisplayName = input.MemberName.Trim();
        user.FamilyGroupId = familyId;
        user.FamilyMemberId = memberId;
        user.Role = OwnerRole;
        db.Categories.AddRange(FamilySeedData.BuildDefaultCategories(familyId));
        await db.SaveChangesAsync(cancellationToken);

        return AccountActionResult.Success();
    }

    public async Task<AccountActionResult> JoinFamilyForUserAsync(Guid userId, JoinFamilyInput input, CancellationToken cancellationToken = default)
    {
        var accessCode = NormalizeAccessCode(input.AccessCode);
        if (string.IsNullOrWhiteSpace(accessCode))
        {
            return AccountActionResult.Fail("Informe o codigo da familia.");
        }

        if (string.IsNullOrWhiteSpace(input.MemberName))
        {
            return AccountActionResult.Fail("Informe o nome para uso na familia.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.AppUsers.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return AccountActionResult.Fail("Usuario nao encontrado.");
        }

        if (user.FamilyGroupId is not null)
        {
            return AccountActionResult.Fail("Este usuario ja esta vinculado a uma familia.");
        }

        var family = await db.FamilyGroups.FirstOrDefaultAsync(item => item.AccessCode == accessCode, cancellationToken);
        if (family is null)
        {
            return AccountActionResult.Fail("Codigo da familia invalido.");
        }

        var memberId = Guid.NewGuid();
        db.FamilyMembers.Add(new FamilyMemberEntity
        {
            Id = memberId,
            FamilyGroupId = family.Id,
            Name = input.MemberName.Trim(),
            Email = user.Email,
            MonthlyIncomeCents = 0,
            ExpenseGoalCents = 0,
            InvestmentGoalCents = 0,
            IsPrimary = false
        });

        user.DisplayName = input.MemberName.Trim();
        user.FamilyGroupId = family.Id;
        user.FamilyMemberId = memberId;
        user.Role = MemberRole;
        await db.SaveChangesAsync(cancellationToken);

        return AccountActionResult.Success();
    }

    private AppUserEntity CreateUser(
        string displayName,
        string email,
        string normalizedEmail,
        string password,
        Guid familyId,
        Guid memberId,
        string role)
    {
        var user = new AppUserEntity
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName.Trim(),
            FamilyGroupId = familyId,
            FamilyMemberId = memberId,
            Role = role,
            CreatedUtc = DateTime.UtcNow,
            LastLoginUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        return user;
    }

    private static bool ValidateCommonRegistration(string displayName, string normalizedEmail, string password, out string error)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            error = "Informe seu nome.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            error = "Informe um e-mail valido.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            error = "Use uma senha com pelo menos 6 caracteres.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static string NormalizeAccessCode(string accessCode) => accessCode.Trim().ToUpperInvariant();

    private static async Task<bool> EmailExistsAsync(AppDbContext db, string normalizedEmail, CancellationToken cancellationToken) =>
        await db.AppUsers.AnyAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);

    private static async Task<string> GenerateUniqueAccessCodeAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        while (true)
        {
            var candidate = GenerateAccessCode();
            var exists = await db.FamilyGroups.AnyAsync(item => item.AccessCode == candidate, cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }
    }

    private static string GenerateAccessCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }
}

public sealed record AccountLoginResult(bool Succeeded, string ErrorMessage, AppUserEntity? User)
{
    public static AccountLoginResult Success(AppUserEntity user) => new(true, string.Empty, user);

    public static AccountLoginResult Fail(string errorMessage) => new(false, errorMessage, null);
}

public sealed record AccountRegistrationResult(bool Succeeded, string ErrorMessage, AppUserEntity? User)
{
    public static AccountRegistrationResult Success(AppUserEntity user) => new(true, string.Empty, user);

    public static AccountRegistrationResult Fail(string errorMessage) => new(false, errorMessage, null);
}

public sealed record AccountActionResult(bool Succeeded, string ErrorMessage)
{
    public static AccountActionResult Success() => new(true, string.Empty);

    public static AccountActionResult Fail(string errorMessage) => new(false, errorMessage);
}

public sealed class NewFamilyRegistrationInput
{
    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FamilyName { get; set; } = string.Empty;
}

public sealed class ExistingFamilyRegistrationInput
{
    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string AccessCode { get; set; } = string.Empty;
}

public sealed class CreateFamilyInput
{
    public string FamilyName { get; set; } = string.Empty;

    public string MemberName { get; set; } = string.Empty;
}

public sealed class JoinFamilyInput
{
    public string AccessCode { get; set; } = string.Empty;

    public string MemberName { get; set; } = string.Empty;
}
