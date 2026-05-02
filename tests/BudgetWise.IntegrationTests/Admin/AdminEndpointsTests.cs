using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using BudgetWise.Application.Admin.DTOs;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.Admin;

public sealed class AdminEndpointsTests(BudgetWiseWebFactory factory)
    : IClassFixture<BudgetWiseWebFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();
    private readonly Faker _faker = new("pt_BR");

    private async Task<AuthResponse> AuthenticateAsync()
    {
        var email = _faker.Internet.Email();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(_faker.Name.FullName(), email, "Senha@123", "Senha@123"));
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private async Task<AuthResponse> AuthenticateAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginUserRequest("admin@test.com", "Admin@123456"));
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    // ── GET /admin/users ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/admin/users?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_AsRegularUser_Returns403()
    {
        var auth = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/users?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_Returns200()
    {
        var auth = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/users?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedList<AdminUserResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Items.Should().NotBeEmpty();
    }

    // ── GET /admin/users/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUserById_AsAdmin_Returns200()
    {
        var regular = await AuthenticateAsync();
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.GetAsync($"/api/v1/admin/users/{regular.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdminUserDetailResponse>(JsonOptions);
        body!.Id.Should().Be(regular.UserId);
    }

    [Fact]
    public async Task GetUserById_AsAdmin_WhenNotFound_Returns404()
    {
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.GetAsync($"/api/v1/admin/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_AsRegularUser_Returns403()
    {
        var auth = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.GetAsync($"/api/v1/admin/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PATCH /admin/users/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_AsAdmin_Returns204()
    {
        var regular = await AuthenticateAsync();
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/users/{regular.UserId}",
            new UpdateUserRequest("Novo Nome", regular.Email));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateUser_AsAdmin_InvalidData_Returns400()
    {
        var regular = await AuthenticateAsync();
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/users/{regular.UserId}",
            new UpdateUserRequest("", "invalid-email"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_AsAdmin_DuplicateEmail_Returns409()
    {
        var regular = await AuthenticateAsync();
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/users/{regular.UserId}",
            new UpdateUserRequest("Novo Nome", "admin@test.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateUser_AsRegularUser_Returns403()
    {
        var auth = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}",
            new UpdateUserRequest("Nome", "email@test.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PATCH /admin/users/{id}/toggle-status ─────────────────────────────────

    [Fact]
    public async Task ToggleStatus_AsAdmin_Returns204()
    {
        var regular = await AuthenticateAsync();
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsync($"/api/v1/admin/users/{regular.UserId}/toggle-status", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ToggleStatus_AsAdmin_WhenNotFound_Returns404()
    {
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsync($"/api/v1/admin/users/{Guid.NewGuid()}/toggle-status", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleStatus_AsRegularUser_Returns403()
    {
        var auth = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.PatchAsync($"/api/v1/admin/users/{Guid.NewGuid()}/toggle-status", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PATCH /admin/users/{id}/unlock ────────────────────────────────────────

    [Fact]
    public async Task UnlockUser_AsAdmin_Returns204()
    {
        var regular = await AuthenticateAsync();
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsync($"/api/v1/admin/users/{regular.UserId}/unlock", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnlockUser_AsAdmin_WhenNotFound_Returns404()
    {
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsync($"/api/v1/admin/users/{Guid.NewGuid()}/unlock", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnlockUser_AsRegularUser_Returns403()
    {
        var auth = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.PatchAsync($"/api/v1/admin/users/{Guid.NewGuid()}/unlock", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PATCH /admin/users/{id}/role ──────────────────────────────────────────

    [Fact]
    public async Task SetRole_AsAdmin_Returns204()
    {
        var regular = await AuthenticateAsync();
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/users/{regular.UserId}/role",
            new SetUserRoleRequest("Admin"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetRole_AsAdmin_InvalidRole_Returns400()
    {
        var regular = await AuthenticateAsync();
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/users/{regular.UserId}/role",
            new SetUserRoleRequest("SuperAdmin"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetRole_AsAdmin_SelfDemote_Returns409()
    {
        var admin = await AuthenticateAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/users/{admin.UserId}/role",
            new SetUserRoleRequest("User"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SetRole_AsRegularUser_Returns403()
    {
        var auth = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/role",
            new SetUserRoleRequest("Admin"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
