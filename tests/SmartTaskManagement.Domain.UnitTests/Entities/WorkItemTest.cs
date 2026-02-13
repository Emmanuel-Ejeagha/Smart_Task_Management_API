using FluentAssertions;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Domain.Events;
using SmartTaskManagement.Domain.Exceptions;

namespace SmartTaskManagement.Domain.UnitTests.Entities;

public class WorkItemTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private const string CreatedBy = "user@test.com";

    [Fact]
    public void Create_WithValidParameters_ShouldRaiseCreatedEvent()
    {
        // Arrange
        var title = "Test WorkItem";
        var description = "Test Description";
        var priority = WorkItemPriority.High;
        var dueDate = DateTime.UtcNow.AddDays(7);

        // Act
        var workItem = new WorkItem(_tenantId, title, description, priority, dueDate, CreatedBy);

        // Assert
        workItem.Should().NotBeNull();
        workItem.Title.Should().Be(title);
        workItem.Description.Should().Be(description);
        workItem.Priority.Should().Be(priority);
        workItem.DueDateUtc.Should().Be(dueDate);
        workItem.TenantId.Should().Be(_tenantId);
        workItem.CreatedBy.Should().Be(CreatedBy);
        workItem.State.Should().Be(WorkItemState.Draft);
        workItem.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkItemCreatedDomainEvent>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        // Act
        Action act = () => new WorkItem(_tenantId, invalidTitle, null, WorkItemPriority.Medium, null, CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new WorkItem(Guid.Empty, "Title", null, WorkItemPriority.Medium, null, CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void Update_WhenArchived_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var workItem = new WorkItem(_tenantId, "Title", null, WorkItemPriority.Medium, null, CreatedBy);
        workItem.Archive(CreatedBy);

        // Act
        Action act = () => workItem.Update("New Title", null, WorkItemPriority.High, DateTime.UtcNow, 5, CreatedBy);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot modify an archived work item*");
    }

    [Fact]
    public void Start_WhenInDraft_ShouldTransitionToInProgress()
    {
        // Arrange
        var workItem = new WorkItem(_tenantId, "Title", null, WorkItemPriority.Medium, null, CreatedBy);

        // Act
        workItem.Start(CreatedBy);

        // Assert
        workItem.State.Should().Be(WorkItemState.InProgress);
        workItem.DomainEvents.Should().Contain(e => e is WorkItemStateChangedDomainEvent);
    }

    [Fact]
    public void Complete_WhenInProgress_ShouldTransitionToCompleted()
    {
        // Arrange
        var workItem = new WorkItem(_tenantId, "Title", null, WorkItemPriority.Medium, null, CreatedBy);
        workItem.Start(CreatedBy);

        // Act
        workItem.Complete(CreatedBy, actualHours: 10);

        // Assert
        workItem.State.Should().Be(WorkItemState.Completed);
        workItem.ActualHours.Should().Be(10);
        workItem.CompletedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddTag_WhenNotArchived_ShouldAddTag()
    {
        // Arrange
        var workItem = new WorkItem(_tenantId, "Title", null, WorkItemPriority.Medium, null, CreatedBy);
        var tag = "urgent";

        // Act
        workItem.AddTag(tag, CreatedBy);

        // Assert
        workItem.Tags.Should().Contain(tag);
    }

    [Fact]
    public void AddReminder_WhenNotArchived_ShouldAddReminder()
    {
        // Arrange
        var workItem = new WorkItem(_tenantId, "Title", null, WorkItemPriority.Medium, null, CreatedBy);
        var reminder = new Reminder(workItem.Id, DateTime.UtcNow.AddDays(1), "Remind me", CreatedBy);

        // Act
        workItem.AddReminder(reminder, CreatedBy);

        // Assert
        workItem.Reminders.Should().ContainSingle()
            .Which.Should().Be(reminder);
        workItem.DomainEvents.Should().Contain(e => e is ReminderScheduledDomainEvent);
    }

    [Fact]
    public void IsOverdue_WhenDueDatePassedAndNotCompleted_ShouldReturnTrue()
    {
        // Arrange
        var pastDueDate = DateTime.UtcNow.AddDays(-1);
        var workItem = new WorkItem(_tenantId, "Title", null, WorkItemPriority.Medium, pastDueDate, CreatedBy);

        // Act
        var isOverdue = workItem.IsOverdue();

        // Assert
        isOverdue.Should().BeTrue();
    }
}