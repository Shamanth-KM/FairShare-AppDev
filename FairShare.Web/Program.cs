using FairShare.Web.Data;
using FairShare.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// ---- Database provider toggle (Sqlite locally, SqlServer in Azure) ----
var provider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// External API (fetch-only currency rates)
builder.Services.AddHttpClient<ICurrencyRateService, CurrencyRateService>(client =>
{
    var baseUrl = builder.Configuration["ExternalApi:BaseUrl"] ?? "https://api.exchangerate.host/";
    client.BaseAddress = new Uri(baseUrl);
    var timeout = int.TryParse(builder.Configuration["ExternalApi:TimeoutSeconds"], out var t) ? t : 15;
    client.Timeout = TimeSpan.FromSeconds(timeout);
});

var app = builder.Build();
Console.WriteLine($"DB Provider => {builder.Configuration["DatabaseProvider"]}");

// DEV ONLY: apply migrations + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
