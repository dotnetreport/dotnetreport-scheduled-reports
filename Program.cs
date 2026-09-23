// ASP.NET Core host with Dotnet Report's scheduled-report job enabled.
//
// The scheduler (Quartz.NET) ships with the DotnetReport NuGet package. All you do is
// start it after the app is built and tell it this app's public URL, which it uses
// to render PDF reports through the report's print view.

using Microsoft.AspNetCore.Authentication.Cookies;
using ReportBuilder.Web.Jobs;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddControllersWithViews();
services.AddHttpClient();
services.AddHttpContextAccessor();
services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Home/Login";
        o.ExpireTimeSpan = TimeSpan.FromDays(30);
        o.SlidingExpiration = true;
    });

var app = builder.Build();

// --- Scheduled reports -----------------------------------------------------
// Starts the Quartz job that polls every 60 seconds, evaluates each saved
// schedule's cron expression (in its own time zone), renders the report as
// PDF / Excel / Link with the schedule's saved user + DataFilters, and emails it
// using the "email" section of appsettings.json.
JobScheduler.Start();

// Public URL of THIS application. PDF rendering opens
// {WebAppRootUrl}/DotnetReport/ReportPrint in a headless browser.
JobScheduler.WebAppRootUrl = builder.Configuration["App:PublicUrl"] ?? "https://localhost:5001";
// ---------------------------------------------------------------------------

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
