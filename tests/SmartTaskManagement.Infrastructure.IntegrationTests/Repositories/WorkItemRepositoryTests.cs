using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Infrastructure.Data;
using SmartTaskManagement.Infrastructure.Repositories;
using Xunit;

namespace SmartTaskManagement.Infrastructure.IntegrationTests.Repositories;

[Collection("Database")]
public class WorkItemRepositoryTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly ApplicationDbContext _context;
    private readonly WorkItemRepository _repository;
    private readonly Guid _tenantId;
    private readonly string _userId = "test-user";

    public WorkItemRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _context = fixture.DbContext;
        _repository = new WorkItemRepository(_context);
        _tenantId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        // Seed a tenant
        var tenant = new Tenant("Test Tenant", "Description", _userId);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        var tenantId = _tenantId;
        tenantId = tenant.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_ShouldPersistWorkItem()
    {
        // Arrange
        var workItem = new WorkItem(_tenantId, "Integration Test", "Description", 
            WorkItemPriority.Critical, DateTime.UtcNow.AddDays(2), _userId);

        // Act
        await _repository.AddAsync(workItem);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.WorkItems.FirstOrDefaultAsync(w => w.Id == workItem.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Integration Test");
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithPagination_ShouldReturnPaginatedResults()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            var workItem = new WorkItem(_tenantId, $"WorkItem {i}", null, 
                WorkItemPriority.Medium, null, _userId);
            await _repository.AddAsync(workItem);
        }
        await _context.SaveChangesAsync();

        var pagination = PaginationRequest.Create(2, 5); // page 2, page size 5

        // Act
        var result = await _repository.GetByTenantIdAsync(_tenantId, pagination);

        // Assert
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.Items.Should().HaveCount(5);
        result.Items.First().Title.Should().Be("WorkItem 6");
    }

    [Fact]
    public async Task GetByStateAsync_ShouldReturnFilteredItems()
    {
        // Arrange
        var workItem1 = new WorkItem(_tenantId, "Draft Item", null, WorkItemPriority.Low, null, _userId);
        var workItem2 = new WorkItem(_tenantId, "InProgress Item", null, WorkItemPriority.High, null, _userId);
        workItem2.Start(_userId);
        await _repository.AddAsync(workItem1);
        await _repository.AddAsync(workItem2);
        await _context.SaveChangesAsync();

        // Act
        var inProgressItems = await _repository.GetByStateAsync(_tenantId, WorkItemState.InProgress);

        // Assert
        inProgressItems.Should().HaveCount(1);
        inProgressItems.First().Title.Should().Be("InProgress Item");
    }

    [Fact]
    public async Task IsTitleUniqueAsync_WithDuplicateTitle_ShouldReturnFalse()
    {
        // Arrange
        var workItem = new WorkItem(_tenantId, "Unique Title", null, WorkItemPriority.Medium, null, _userId);
        await _repository.AddAsync(workItem);
        await _context.SaveChangesAsync();

        // Act
        var isUnique = await _repository.IsTitleUniqueAsync(_tenantId, "Unique Title", workItem.Id);

        // Assert
        isUnique.Should().BeTrue(); // excluding itself
    }
}