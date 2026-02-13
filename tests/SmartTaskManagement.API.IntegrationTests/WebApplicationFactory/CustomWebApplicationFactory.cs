using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Npgsql;
using Respawn;
using SmartTaskManagement.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace SmartTaskManagement.API.IntegrationTests.WebApplicationFactory;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private Respawner _respawner = null!;
    private NpgsqlConnection _dbConnection = null!;

    public string TenantId => TestAuthHandler.TenantId;
    public string UserId => TestAuthHandler.UserId;

    public CustomWebApplicationFactory()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("SmartTaskManagement_Test")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .WithCleanUp(true)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString(),
                ["ConnectionStrings:HangfireConnection"] = _container.GetConnectionString(),
                ["Auth0:Domain"] = "test",
                ["Auth0:Audience"] = "test",
                ["Auth0:ClientId"] = "test",
                ["Auth0:ClientSecret"] = "test"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove existing authentication and add test authentication
            services.RemoveAll<IAuthenticationSchemeProvider>();
            services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme, options => { });

            // Ensure no real DbContext is registered
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_container.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Also create Hangfire database
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand("CREATE DATABASE Hangfire;", connection);
        await cmd.ExecuteNonQueryAsync();

        // Apply EF migrations
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        // Seed test tenant and maybe default data
        // We can also let the seed happen via the app startup

        // Setup Respawn
        _dbConnection = new NpgsqlConnection(_container.GetConnectionString());
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public new async Task DisposeAsync()
    {
        await _dbConnection?.CloseAsync()!;
        await _container.DisposeAsync();
    }
}