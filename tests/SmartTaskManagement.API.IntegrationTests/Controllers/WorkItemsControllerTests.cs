using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartTaskManagement.API.IntegrationTests.WebApplicationFactory;
using SmartTaskManagement.API.Models;
using SmartTaskManagement.Application.Features.WorkItems.Commands.CreateWorkItem;
using SmartTaskManagement.Application.Features.WorkItems.Dtos;
using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.API.IntegrationTests.Controllers;

[Collection("Database")]
public class WorkItemsControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WorkItemsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("Admin", "true"); // For admin access
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateWorkItem_WithValidData_ReturnsCreated()
    {
        // Arrange
        var command = new CreateWorkItemCommand
        {
            Title = "Integration Test WorkItem",
            Description = "Created via API test",
            Priority = WorkItemPriority.High,
            DueDateUtc = DateTime.UtcNow.AddDays(5),
            EstimatedHours = 8,
            Tags = new List<string> { "test", "integration" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/workitems", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWorkItemById_WhenExists_ReturnsWorkItem()
    {
        // Arrange – create a work item first
        var createCommand = new CreateWorkItemCommand
        {
            Title = "Get Test",
            Description = "Test",
            Priority = WorkItemPriority.Medium,
            DueDateUtc = DateTime.UtcNow.AddDays(3),
            EstimatedHours = 5,
            Tags = new List<string>()
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workitems", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var workItemId = createResult!.Data;

        // Act
        var response = await _client.GetAsync($"/api/v1/workitems/{workItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WorkItemDto>>();
        result.Should().NotBeNull();
        result!.Data.Title.Should().Be("Get Test");
    }

    [Fact]
    public async Task DeleteWorkItem_AsAdmin_ShouldReturnSuccess()
    {
        // Arrange – create a work item
        var createCommand = new CreateWorkItemCommand
        {
            Title = "Delete Test",
            Description = "To be deleted",
            Priority = WorkItemPriority.Low,
            DueDateUtc = null,
            EstimatedHours = 0,
            Tags = new List<string>()
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workitems", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var workItemId = createResult!.Data;

        // Act
        var response = await _client.DeleteAsync($"/api/v1/workitems/{workItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteWorkItem_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange – remove admin header
        _client.DefaultRequestHeaders.Remove("Admin");
        var createCommand = new CreateWorkItemCommand
        {
            Title = "Delete Test Forbidden",
            Description = "Should fail",
            Priority = WorkItemPriority.Low,
            DueDateUtc = null,
            EstimatedHours = 0,
            Tags = new List<string>()
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workitems", createCommand);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var workItemId = createResult!.Data;

        // Act
        var response = await _client.DeleteAsync($"/api/v1/workitems/{workItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}