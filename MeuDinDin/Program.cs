using System.Security.Claims;
using MeuDinDin.Components;
using MeuDinDin.Data;
using MeuDinDin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dataProtectionDirectory = builder.Configuration["MEUDINDIN_KEYS_DIR"];
var keysDirectory = string.IsNullOrWhiteSpace(dataProtectionDirectory)
    ? Path.Combine(builder.Environment.ContentRootPath, "App_Keys")
    : Path.GetFullPath(dataProtectionDirectory);
Directory.CreateDirectory(keysDirectory);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["MEUDINDIN_DATABASE_URL"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("A connection string 'DefaultConnection' ou a variavel 'MEUDINDIN_DATABASE_URL' deve ser configurada.");
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("MeuDinDin");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/entrar";
        options.AccessDeniedPath = "/entrar";
        options.Cookie.Name = "MeuDinDin.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
    });
builder.Services.AddAuthorization();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IFinanceHub, FinanceHub>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IPasswordHasher<AppUserEntity>, PasswordHasher<AppUserEntity>>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var dbContext = dbFactory.CreateDbContext();
    dbContext.Database.EnsureCreated();
    AppDbSchema.EnsureLatest(dbContext);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapPost("/auth/login", async (HttpContext httpContext, IAccountService accountService) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var returnUrl = SanitizeReturnUrl(form["returnUrl"].ToString());
    var result = await accountService.ValidateCredentialsAsync(form["email"].ToString(), form["password"].ToString());
    if (!result.Succeeded || result.User is null)
    {
        return Results.LocalRedirect(BuildRedirect("/entrar", ("erro", result.ErrorMessage), ("returnUrl", returnUrl)));
    }

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        BuildPrincipal(result.User));

    return Results.LocalRedirect(returnUrl);
});

app.MapPost("/auth/register", async (HttpContext httpContext, IAccountService accountService) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var mode = form["mode"].ToString();
    var returnUrl = SanitizeReturnUrl(form["returnUrl"]);

    AccountRegistrationResult result = mode == "join"
        ? await accountService.RegisterWithExistingFamilyAsync(new ExistingFamilyRegistrationInput
        {
            DisplayName = form["displayName"].ToString(),
            Email = form["email"].ToString(),
            Password = form["password"].ToString(),
            AccessCode = form["accessCode"].ToString()
        })
        : await accountService.RegisterWithNewFamilyAsync(new NewFamilyRegistrationInput
        {
            DisplayName = form["displayName"].ToString(),
            Email = form["email"].ToString(),
            Password = form["password"].ToString(),
            FamilyName = form["familyName"].ToString()
        });

    if (!result.Succeeded || result.User is null)
    {
        return Results.LocalRedirect(BuildRedirect("/cadastro", ("erro", result.ErrorMessage), ("modo", mode), ("returnUrl", returnUrl)));
    }

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        BuildPrincipal(result.User));

    return Results.LocalRedirect(returnUrl);
});

app.MapPost("/auth/family/create", async (HttpContext httpContext, IAccountService accountService) =>
{
    var userId = GetUserId(httpContext.User);
    if (userId is null)
    {
        return Results.LocalRedirect("/entrar");
    }

    var form = await httpContext.Request.ReadFormAsync();
    var result = await accountService.CreateFamilyForUserAsync(userId.Value, new CreateFamilyInput
    {
        FamilyName = form["familyName"].ToString(),
        MemberName = form["memberName"].ToString()
    });

    return result.Succeeded
        ? Results.LocalRedirect("/")
        : Results.LocalRedirect(BuildRedirect("/", ("erro", result.ErrorMessage)));
});

app.MapPost("/auth/family/join", async (HttpContext httpContext, IAccountService accountService) =>
{
    var userId = GetUserId(httpContext.User);
    if (userId is null)
    {
        return Results.LocalRedirect("/entrar");
    }

    var form = await httpContext.Request.ReadFormAsync();
    var result = await accountService.JoinFamilyForUserAsync(userId.Value, new JoinFamilyInput
    {
        AccessCode = form["accessCode"].ToString(),
        MemberName = form["memberName"].ToString()
    });

    return result.Succeeded
        ? Results.LocalRedirect("/")
        : Results.LocalRedirect(BuildRedirect("/", ("erro", result.ErrorMessage)));
});

app.MapPost("/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/entrar");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static ClaimsPrincipal BuildPrincipal(AppUserEntity user)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.DisplayName),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.Role)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    return new ClaimsPrincipal(identity);
}

static Guid? GetUserId(ClaimsPrincipal user)
{
    var rawId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(rawId, out var userId) ? userId : null;
}

static string SanitizeReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        return "/";
    }

    return returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal)
        ? returnUrl
        : "/";
}

static string BuildRedirect(string path, params (string Key, string? Value)[] values)
{
    var query = values
        .Where(item => !string.IsNullOrWhiteSpace(item.Value))
        .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}")
        .ToArray();

    return query.Length == 0
        ? path
        : $"{path}?{string.Join("&", query)}";
}
