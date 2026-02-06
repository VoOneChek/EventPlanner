using EventPlannerWebApplication.Data;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("EVENT_DB_CONNECTION");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Переменная среды EVENT_DB_CONNECTION не задана");
}

builder.Services.AddDbContext<EventPlannerDbContext>(options =>
    options.UseNpgsql(connectionString,
        b => b.MigrationsAssembly(typeof(EventPlannerDbContext).Assembly.FullName)));

builder.Services.AddSession();
builder.Services.AddScoped<EmailService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
