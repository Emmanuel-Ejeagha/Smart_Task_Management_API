using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using SmartTaskManagement.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SmartTaskManagement.Infrastructure.IntegrationTests;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private string _connectionString = null!;
    private Respawner _respawner = null!;
    private NpgsqlConnection _dbConnection = null!;

    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public ApplicationDbContext DbContext { get; private set; } = null!;

    public TestDatabaseFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("SmartTaskManagement_Test")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        // Also create Hangfire database
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand("CREATE DATABASE Hangfire;", connection);
        await cmd.ExecuteNonQueryAsync();

        // Setup DbContext
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(_connectionString));
        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Apply migrations
        await DbContext.Database.MigrateAsync();

        // Setup Respawn for clean state between tests
        _dbConnection = new NpgsqlConnection(_connectionString);
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner != null && _dbConnection != null)
        {
            await _respawner.ResetAsync(_dbConnection);
        }
    }

    public async Task DisposeAsync()
    {
        await _dbConnection?.CloseAsync()!;
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture> { }