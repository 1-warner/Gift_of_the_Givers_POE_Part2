using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF community licence (free for this use)
QuestPDF.Settings.License = LicenseType.Community;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity with roles. Email confirmation is off for the prototype so login works immediately.
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// Part 2, Section A: a named HttpClient for calling the GiftOfTheGivers.Functions app.
// Functions:BaseUrl -> http://localhost:7071/api/ when running the Functions project locally
// (func start / F5 in Visual Studio), or the deployed Function App's /api/ URL in Azure.
// Functions:Key is only needed once the Function is deployed with AuthorizationLevel.Function
// (the local host does not require it) - set it via user-secrets or App Service configuration,
// never committed to source.
builder.Services.AddHttpClient("FunctionsClient", client =>
{
    var baseUrl = builder.Configuration["Functions:BaseUrl"] ?? "http://localhost:7071/api/";
    client.BaseAddress = new Uri(baseUrl);

    var functionsKey = builder.Configuration["Functions:Key"];
    if (!string.IsNullOrWhiteSpace(functionsKey))
    {
        client.DefaultRequestHeaders.Add("x-functions-key", functionsKey);
    }
});

var app = builder.Build();

// Ensure the database exists and is seeded with roles, demo users and a demo project.
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
