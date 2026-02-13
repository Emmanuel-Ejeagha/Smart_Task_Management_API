using FluentAssertions;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Domain.Services;

namespace SmartTaskManagement.Domain.UnitTests.Services;

public class WorkItemDomainServiceTests
{
    private readonly WorkItemDomainService _service;
    private readonly WorkItem _workItem;
    private readonly Guid _tenantId = Guid.NewGuid();
    private const string CreatedBy = "user@test.com";

    public WorkItemDomainServiceTests()
    {
        _service = new WorkItemDomainService();
        _workItem = new WorkItem(_tenantId, "Test", null, WorkItemPriority.Medium, null, CreatedBy);
    }

    [Fact]
    public void CanTransitionToState_FromDraftToInProgress_ShouldReturnTrue()
    {
        // Arrange
        _workItem.State = WorkItemState.Draft;

        // Act
        var canTransition = _service.CanTransitionToState(_workItem, WorkItemState.InProgress);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Fact]
    public void CanTransitionToState_FromArchivedToAny_ShouldReturnFalse()
    {
        // Arrange
        _workItem.State = WorkItemState.Archived;

        // Act
        var canTransition = _service.CanTransitionToState(_workItem, WorkItemState.Draft);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void CalculateProgressPercentage_WithEstimatedHoursZero_ShouldReturnZero()
    {
        // Arrange
        _workItem.EstimatedHours = 0;

        // Act
        var progress = _service.CalculateProgressPercentage(_workItem);

        // Assert
        progress.Should().Be(0);
    }

    [Fact]
    public void CalculateProgressPercentage_WithHalfWorkDone_ShouldReturn50()
    {
        // Arrange
        _workItem.EstimatedHours = 10;
        _workItem.ActualHours = 5;

        // Act
        var progress = _service.CalculateProgressPercentage(_workItem);

        // Assert
        progress.Should().Be(50);
    }

    [Fact]
    public void CanScheduleReminder_WhenWorkItemArchived_ShouldReturnFalse()
    {
        // Arrange
        _workItem.Archive(CreatedBy);
        var futureDate = DateTime.UtcNow.AddHours(1);

        // Act
        var canSchedule = _service.CanScheduleReminder(_workItem, futureDate);

        // Assert
        canSchedule.Should().BeFalse();
    }

    [Fact]
    public void CanScheduleReminder_WhenReminderDateInPast_ShouldReturnFalse()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddHours(-1);

        // Act
        var canSchedule = _service.CanScheduleReminder(_workItem, pastDate);

        // Assert
        canSchedule.Should().BeFalse();
    }
}