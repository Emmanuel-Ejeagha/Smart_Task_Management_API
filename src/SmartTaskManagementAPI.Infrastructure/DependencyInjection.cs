using System;
using System.Runtime.InteropServices;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.SendTaskReminder;
using SmartTaskManagementAPI.Application.Features.Tasks.Queries.TenantAccessChecker;
using SmartTaskManagementAPI.Application.Interfaces;
using SmartTaskManagementAPI.Infrastructure.BackgroundJobs;
using SmartTaskManagementAPI.Infrastructure.Data;
using SmartTaskManagementAPI.Infrastructure.Data.Repositories;
using SmartTaskManagementAPI.Infrastructure.Identity;
using SmartTaskManagementAPI.Infrastructure.Services;

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
            // Passsword settings
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settingd
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

                    return true;
                }));
        });

        // Repositories
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<ITenantAccessChecker, TenantAccessChecker>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<RecurringJobsService>();

        // Backgroumd Jobs
        services.AddScoped<TaskReminderJob>();

         /// Background Jobs - Hangfire
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
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
                        UseNativeDatabaseTransactions = true,
                    });
        });

        services.AddHangfireServer(options =>
        {
            options.ServerName = $"SmartTaskManagementAPI-{Environment.MachineName}";
            options.WorkerCount = Environment.ProcessorCount * 5;
            options.Queues = new[] { "default", "critical", "low" };
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });


        // Background Job Services
        services.AddScoped<TaskReminderJob>();
        services.AddScoped<OverdueTaskNotificationJob>();
        services.AddScoped<DatabaseCleanupJob>();
        services.AddScoped<SendTaskReminderCommandHandler>();
        
        // Email Service
        services.AddScoped<IEmailService, EmailService>();
        
        // Recurring Jobs Service (as hosted service)
        services.AddSingleton<RecurringJobsService>();
        services.AddHostedService(provider => provider.GetRequiredService<RecurringJobsService>());

        return services;        
    }
}
