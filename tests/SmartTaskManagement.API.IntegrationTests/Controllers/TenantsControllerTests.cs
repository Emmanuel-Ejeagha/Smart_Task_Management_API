using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartTaskManagement.API.IntegrationTests.WebApplicationFactory;
using SmartTaskManagement.API.Models;
using SmartTaskManagement.Application.Features.Tenants.Dtos;

namespace SmartTaskManagement.API.IntegrationTests.Controllers;

[Collection("Database")]
public class TenantsControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TenantsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("Admin", "true");
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetCurrentTenant_ShouldReturnTenantFromClaim()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/tenants/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TenantDto>>();
        result.Should().NotBeNull();
        result!.Data.Id.Should().Be(Guid.Parse(_factory.TenantId));
    }
}