using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SendGrid;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Infrastructure.Authentication;
using SmartTaskManagement.Infrastructure.BackgroundJobs;
using SmartTaskManagement.Infrastructure.Data;
using SmartTaskManagement.Infrastructure.Repositories;
using SmartTaskManagement.Infrastructure.Services;

namespace SmartTaskManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddRepositories();
        services.AddAuthentication(configuration);
        services.AddBackgroundJobs(configuration);
        services.AddInfrastructureServices();
        services.AddHttpContextAccessor();
        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ApplicationDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var auth0Settings = configuration.GetSection("Auth0");
        services.Configure<Auth0Settings>(auth0Settings);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Settings["Domain"]}/";
            options.Audience = auth0Settings["Audience"];
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.EventsType = typeof(JwtBearerEventsHandler); // ✅ Correct
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("User", policy => policy.RequireRole("User", "Admin"));
            options.AddPolicy("TenantAccess", policy =>
                policy.RequireAssertion(context =>
                {
                    var tenantId = context.User.FindFirst("tenantId")?.Value;
                    return !string.IsNullOrEmpty(tenantId);
                }));
        });

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<Auth0ClaimsPrincipalFactory>();
        services.AddScoped<JwtBearerEventsHandler>();
    }

    private static void AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var hangfireConnectionString = configuration.GetConnectionString("HangfireConnection")
            ?? configuration.GetConnectionString("DefaultConnection");

        /*
    Source - https://stackoverflow.com/a/78529481
    Posted by Sergey M.
    Retrieved 2026-02-11, License - CC BY-SA 4.0 */
        services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnectionString))
    );


        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = new[] { "default", "reminders", "emails" };
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
        services.AddScoped<ReminderJob>();
        services.AddScoped<DueRemindersCheckJob>();
        services.AddSingleton<HangfireDashboardAuthorizationFilter>();
    }

    private static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IDateTimeService, DateTimeService>();

        services.Configure<EmailSettings>(options =>
            options.SendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? string.Empty);

        services.AddSingleton<ISendGridClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;
            return new SendGridClient(settings.SendGridApiKey);
        });

        services.AddScoped<IEmailService, EmailService>();
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // await context.Database.MigrateAsync();
        await ApplicationDbContextSeed.SeedAsync(context);
    }
}

public class Auth0Settings
{
    public string Domain { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class EmailSettings
{
    public string FromEmail { get; set; } = "noreply@smarttaskmanagement.com";
    public string FromName { get; set; } = "Smart Task Management";
    public string SendGridApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}