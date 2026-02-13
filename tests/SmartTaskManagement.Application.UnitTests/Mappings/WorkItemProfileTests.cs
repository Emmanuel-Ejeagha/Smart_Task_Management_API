using AutoMapper;
using FluentAssertions;
using SmartTaskManagement.Application.Features.WorkItems.Dtos;
using SmartTaskManagement.Application.Mappings;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using Xunit;

namespace SmartTaskManagement.Application.UnitTests.Mappings;

public class WorkItemProfileTests
{
    private readonly IMapper _mapper;

    public WorkItemProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<WorkItemProfile>());
        _mapper = config.CreateMapper();
        config.AssertConfigurationIsValid(); // ensures all mappings are correct
    }

    [Fact]
    public void Map_WorkItemToWorkItemDto_ShouldMapCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workItem = new WorkItem(tenantId, "Test Title", "Test Description", 
            WorkItemPriority.High, DateTime.UtcNow.AddDays(1), "user");
        workItem.AddTag("urgent", "user");
        workItem.AddTag("test", "user");
        workItem.Start("user");
        workItem.Complete("user", 5);

        // Act
        var dto = _mapper.Map<WorkItemDto>(workItem);

        // Assert
        dto.Id.Should().Be(workItem.Id);
        dto.Title.Should().Be(workItem.Title);
        dto.Description.Should().Be(workItem.Description);
        dto.Priority.Should().Be(workItem.Priority);
        dto.State.Should().Be(workItem.State);
        dto.DueDateUtc.Should().Be(workItem.DueDateUtc);
        dto.EstimatedHours.Should().Be(workItem.EstimatedHours);
        dto.ActualHours.Should().Be(workItem.ActualHours);
        dto.Tags.Should().BeEquivalentTo(workItem.Tags);
        dto.CreatedAtUtc.Should().Be(workItem.CreatedAtUtc);
        dto.CreatedBy.Should().Be(workItem.CreatedBy);
        dto.CompletedAtUtc.Should().Be(workItem.CompletedAtUtc);
    }
}