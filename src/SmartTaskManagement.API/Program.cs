using Serilog;
using SmartTaskManagement.API.Extensions;
using SmartTaskManagement.Application;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Infrastructure;
using SmartTaskManagement.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("Starting Smart Task Management API");

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseApiServices();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    try
    {
        Log.Information("Seeding database...");
        await ApplicationDbContextSeed.SeedAsync(dbContext);
        Log.Information("Database seeded successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database seeding failed – continuing without seed data.");
    }

    try
    {
        Log.Information("Scheduling background jobs...");
        var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
        backgroundJobService.ScheduleDueRemindersCheck(TimeSpan.FromMinutes(1));
        Log.Information("Background jobs scheduled successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to schedule background jobs – Hangfire may not be ready.");
    }
}

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly after startup");
    throw;
}