using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SmartTaskManagementAPI.API.Extensions;
using SmartTaskManagementAPI.API.Middleware;
using SmartTaskManagementAPI.Infrastructure.Data;
using SmartTaskManagementAPI.Infrastructure.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SmartTaskManagementAPI")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Smart Task Management API");
    
    // Add services
    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();
    
    var app = builder.Build();
    
    // Configure middleware pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => 
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Task Management API v1");
            c.RoutePrefix = "swagger";
        });
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }
    
    // Custom middleware
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    
    app.UseCors("AllowSpecificOrigin");
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();
    
    // Health check endpoint
    app.MapHealthChecks("/health");
    
    // Hangfire dashboard (only if DB is available)
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new SmartTaskManagementAPI.Infrastructure.BackgroundJobs.HangfireDashboardAuthorizationFilter() },
        DashboardTitle = "Smart Task Management Jobs",
        AppPath = "/hangfire",
        DisplayStorageConnectionString = false
    });
    
    // Initialize database with retry logic
    await InitializeDatabaseWithRetryAsync(app);
    
    Log.Information("Smart Task Management API started successfully");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("API Documentation: {Url}/swagger", builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static async Task InitializeDatabaseWithRetryAsync(WebApplication app)
{
    var maxRetries = 5;
    var retryDelay = TimeSpan.FromSeconds(5);
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            Log.Information("Attempting database connection (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
            
            // Test connection first
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                Log.Information("Database connection successful");
                
                // Apply migrations
                await context.Database.MigrateAsync();
                Log.Information("Database migrations applied");
                
                // Seed data
                await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
                Log.Information("Database seeded successfully");
                
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Database connection attempt {Attempt} failed: {Message}", attempt, ex.Message);
            
            if (attempt == maxRetries)
            {
                Log.Error(ex, "Failed to connect to database after {MaxRetries} attempts. Starting without database...", maxRetries);
                break;
            }
            
            Log.Information("Retrying in {Delay} seconds...", retryDelay.TotalSeconds);
            await Task.Delay(retryDelay);
        }
    }
}