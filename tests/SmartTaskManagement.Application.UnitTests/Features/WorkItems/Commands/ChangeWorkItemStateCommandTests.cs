using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.WorkItems.Commands.ChangeWorkItemState;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Domain.Services;
using Xunit;

namespace SmartTaskManagement.Application.UnitTests.Features.WorkItems.Commands;

public class ChangeWorkItemStateCommandTests
{
    private readonly Mock<IWorkItemRepository> _workItemRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IWorkItemService> _workItemServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ChangeWorkItemStateCommandHandler _handler;
    private readonly ChangeWorkItemStateCommandValidator _validator;

    public ChangeWorkItemStateCommandTests()
    {
        _workItemRepositoryMock = new Mock<IWorkItemRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _workItemServiceMock = new Mock<IWorkItemService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new ChangeWorkItemStateCommandHandler(
            _workItemRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _workItemServiceMock.Object,
            _unitOfWorkMock.Object);
        _validator = new ChangeWorkItemStateCommandValidator();
    }

    [Fact]
    public async Task Handle_ValidTransition_ShouldUpdateState()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var userId = "user-id";
        var workItem = new WorkItem(tenantId, "Test", null, WorkItemPriority.Medium, null, userId);

        var command = new ChangeWorkItemStateCommand
        {
            Id = workItemId,
            NewState = WorkItemState.InProgress
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _workItemRepositoryMock.Setup(x => x.GetByIdAsync(workItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _workItemServiceMock.Setup(x => x.CanTransitionToState(workItem, WorkItemState.InProgress))
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        workItem.State.Should().Be(WorkItemState.InProgress);
        _workItemRepositoryMock.Verify(x => x.UpdateAsync(workItem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkItemNotFound_ShouldReturnFailure()
    {
        // Arrange
        _workItemRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItem)null!);

        // Act
        var result = await _handler.Handle(new ChangeWorkItemStateCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Work item not found");
    }
}