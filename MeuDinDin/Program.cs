using MeuDinDin.Components;
using Microsoft.EntityFrameworkCore;
using MeuDinDin.Data;
using MeuDinDin.Services;

var builder = WebApplication.CreateBuilder(args);
var configuredDataDirectory = builder.Configuration["MEUDINDIN_DATA_DIR"];
var databaseDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
    ? Path.Combine(builder.Environment.ContentRootPath, "App_Data")
    : Path.GetFullPath(configuredDataDirectory);
Directory.CreateDirectory(databaseDirectory);
var databasePath = Path.Combine(databaseDirectory, "meudindin.db");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddScoped<IFinanceHub, FinanceHub>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var dbContext = dbFactory.CreateDbContext();
    dbContext.Database.EnsureCreated();
    AppDbSchema.EnsureLatest(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
