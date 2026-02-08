using EventPlannerWebApplication.Data;
using EventPlannerWebApplication.Services;
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
builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITimeService,  TimeService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventPlannerDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
