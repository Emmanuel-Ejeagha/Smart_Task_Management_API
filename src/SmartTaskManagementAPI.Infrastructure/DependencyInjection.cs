using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Interfaces;
using SmartTaskManagementAPI.Infrastructure.BackgroundJobs;
using SmartTaskManagementAPI.Infrastructure.Data;
using SmartTaskManagementAPI.Infrastructure.Data.Repositories;
using SmartTaskManagementAPI.Infrastructure.Identity;
using SmartTaskManagementAPI.Infrastructure.Services;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Hosting;

namespace SmartTaskManagementAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            
            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            
            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers["Token-Expired"] = "true";
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Authorization policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => 
                policy.RequireRole("Admin"));
            
            options.AddPolicy("UserOrAdmin", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Admin") || 
                    context.User.IsInRole("User")));
            
            options.AddPolicy("SameTenant", policy =>
                policy.RequireAssertion(context =>
                {
                    if (!context.User.HasClaim(c => c.Type == "TenantId"))
                        return false;
                    
                    // This would be enhanced with tenant-based authorization logic
                    return true;
                }));
        });

        // Repositories (FIXED: All repositories properly registered)
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services (FIXED: All services properly registered)
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        // In the Services section, add:
        services.AddScoped<ITenantAccessChecker, TenantAccessChecker>();
        services.AddSingleton<RecurringJobsService>();

        // Background Jobs (FIXED: All jobs properly registered)
        services.AddScoped<TaskReminderJob>();
        services.AddScoped<OverdueTaskNotificationJob>();
        services.AddScoped<DatabaseCleanupJob>();
        
        // Register HangfireActivator
        services.AddSingleton<JobActivator, HangfireActivator>();

        // Hangfire Configuration (Corrected)
        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(
                options =>
                {
                    options.UseNpgsqlConnection(
                        configuration.GetConnectionString("DefaultConnection"));
                },
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire",
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    UseNativeDatabaseTransactions = true
                });
        });


        services.AddHangfireServer(options =>
        {
            options.ServerName = $"SmartTaskManagementAPI-{Environment.MachineName}";
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = new[] { "default", "critical", "low" };
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });

        // Register RecurringJobsService as hosted service
        services.AddHostedService<RecurringJobsServiceHosted>();

        return services;
    }
}
