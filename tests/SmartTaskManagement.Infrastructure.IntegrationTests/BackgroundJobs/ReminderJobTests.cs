using FluentAssertions;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Infrastructure.BackgroundJobs;
using SmartTaskManagement.Infrastructure.Data;
using Xunit;

namespace SmartTaskManagement.Infrastructure.IntegrationTests.BackgroundJobs;

[Collection("Database")]
public class ReminderJobTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailServiceMock;
    private readonly IMediator _mediatorMock;
    private readonly ReminderJob _job;
    private readonly Guid _tenantId;
    private readonly string _userId = "system";

    public ReminderJobTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _context = fixture.DbContext;

        var emailMock = new Mock<IEmailService>();
        _emailServiceMock = emailMock.Object;

        var mediatorMock = new Mock<IMediator>();
        _mediatorMock = mediatorMock.Object;

        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        dbContextFactoryMock.Setup(x => x.CreateDbContext())
            .Returns(() => new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql(fixture.DbContext.Database.GetConnectionString())
                    .Options));

        _job = new ReminderJob(
            _mediatorMock,
            NullLogger<ReminderJob>.Instance,
            dbContextFactoryMock.Object,
            _emailServiceMock);
        _tenantId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        var tenant = new Tenant("Test Tenant", "Description", _userId);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        var tenantId = _tenantId;
        tenantId = tenant.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProcessReminderAsync_WhenReminderExists_ShouldTriggerReminder()
    {
        // Arrange
        var workItem = new WorkItem(_tenantId, "Test Item", null, WorkItemPriority.Medium, null, _userId);
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var reminder = new Reminder(workItem.Id, DateTime.UtcNow.AddMinutes(-1), "Test Reminder", _userId);
        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();

        // Act
        await _job.ProcessReminderAsync(reminder.Id, JobCancellationToken.Null);

        // Assert
        var processedReminder = await _context.Reminders.FindAsync(reminder.Id);
        processedReminder!.Status.Should().Be(ReminderStatus.Triggered);
        processedReminder.TriggeredAtUtc.Should().NotBeNull();
    }
}