
using AutoMapper;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Features.WorkItems.Commands.CreateWorkItem;
using SmartTaskManagement.Application.Mappings;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using Xunit;

namespace SmartTaskManagement.Application.UnitTests.Features.WorkItems.Commands;

public class CreateWorkItemCommandTests
{
    private readonly Mock<IWorkItemRepository> _workItemRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly CreateWorkItemCommandHandler _handler;
    private readonly CreateWorkItemCommandValidator _validator;

    public CreateWorkItemCommandTests()
    {
        _workItemRepositoryMock = new Mock<IWorkItemRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<WorkItemProfile>());
        _mapper = config.CreateMapper();

        _handler = new CreateWorkItemCommandHandler(
            _workItemRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object);

        _validator = new CreateWorkItemCommandValidator();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateWorkItemAndReturnId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = "test-user-id";
        var command = new CreateWorkItemCommand
        {
            Title = "Test WorkItem",
            Description = "Test Description",
            Priority = WorkItemPriority.High,
            DueDateUtc = DateTime.UtcNow.AddDays(7),
            EstimatedHours = 8,
            Tags = new List<string> { "test", "urgent" }
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _workItemRepositoryMock.Setup(x => x.IsTitleUniqueAsync(tenantId, command.Title, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _workItemRepositoryMock.Verify(x => x.AddAsync(It.Is<WorkItem>(w =>
            w.Title == command.Title &&
            w.TenantId == tenantId &&
            w.CreatedBy == userId
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.DispatchDomainEventsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TitleNotUnique_ShouldReturnFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new CreateWorkItemCommand { Title = "Duplicate Title" };

        _currentUserServiceMock.Setup(x => x.UserId).Returns("user");
        _currentUserServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _workItemRepositoryMock.Setup(x => x.IsTitleUniqueAsync(tenantId, command.Title, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public void Validator_EmptyTitle_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateWorkItemCommand { Title = "" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validator_TitleTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateWorkItemCommand { Title = new string('a', 201) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }
}