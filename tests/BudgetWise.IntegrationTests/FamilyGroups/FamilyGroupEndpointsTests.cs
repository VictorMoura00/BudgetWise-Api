using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.FamilyGroups.DTOs;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.FamilyGroups;

public sealed class FamilyGroupEndpointsTests(BudgetWiseWebFactory factory)
    : IClassFixture<BudgetWiseWebFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();
    private readonly Faker _faker = new("pt_BR");

    private async Task<string> AuthenticateAsync()
    {
        var email = _faker.Internet.Email();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(_faker.Name.FullName(), email, "Senha@123", "Senha@123"));
        var tokens = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return tokens!.AccessToken;
    }

    private async Task<FamilyGroupResponse> CreateGroupAsync(string token, string? name = null)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/family-groups",
            new CreateFamilyGroupRequest(name ?? _faker.Commerce.Department(), _faker.Lorem.Sentence()));
        return (await response.Content.ReadFromJsonAsync<FamilyGroupResponse>(JsonOptions))!;
    }

    private async Task<FamilyGroupResponse> JoinGroupAsync(string token, string inviteCode)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/family-groups/join",
            new JoinFamilyGroupRequest(inviteCode));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FamilyGroupResponse>(JsonOptions))!;
    }

    // ── POST /family-groups ───────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_Returns201()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/family-groups",
            new CreateFamilyGroupRequest("familia-silva", "Grupo da família Silva"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<FamilyGroupResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Name.Should().Be("familia-silva");
        body.InviteCode.Should().NotBeNullOrEmpty();
        body.Members.Should().ContainSingle(m => m.Role == "Owner");
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/family-groups",
            new CreateFamilyGroupRequest("teste", null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_GroupLimitReached_Returns409()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 5; i++)
            await _client.PostAsJsonAsync("/api/v1/family-groups",
                new CreateFamilyGroupRequest($"grupo-{i}", null));

        var response = await _client.PostAsJsonAsync("/api/v1/family-groups",
            new CreateFamilyGroupRequest("grupo-limite", null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── GET /family-groups ────────────────────────────────────────────────────

    [Fact]
    public async Task GetList_Authenticated_ReturnsGroups()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateGroupAsync(token, "meu-grupo");

        var response = await _client.GetAsync("/api/v1/family-groups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<FamilyGroupSummaryResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Count.Should().BeGreaterThanOrEqualTo(1);
        body.Any(g => g.Name == "meu-grupo").Should().BeTrue();
    }

    [Fact]
    public async Task GetList_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/family-groups");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /family-groups/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenExists_Returns200()
    {
        var token = await AuthenticateAsync();
        var group = await CreateGroupAsync(token, "familia-oliveira");

        var response = await _client.GetAsync($"/api/v1/family-groups/{group.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FamilyGroupResponse>(JsonOptions);
        body!.Id.Should().Be(group.Id);
        body.Members.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/family-groups/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WhenNotMember_Returns404()
    {
        var tokenA = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA, "grupo-privado");

        var tokenB = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.GetAsync($"/api/v1/family-groups/{group.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /family-groups/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task Update_AsOwner_Returns200()
    {
        var token = await AuthenticateAsync();
        var group = await CreateGroupAsync(token, "grupo-antigo");

        var response = await _client.PutAsJsonAsync($"/api/v1/family-groups/{group.Id}",
            new UpdateFamilyGroupRequest("grupo-novo", "Nova descrição"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FamilyGroupResponse>(JsonOptions);
        body!.Name.Should().Be("grupo-novo");
        body.Description.Should().Be("Nova descrição");
    }

    [Fact]
    public async Task Update_AsMember_Returns403()
    {
        var tokenA = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA, "grupo-protegido");

        var tokenB = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.PutAsJsonAsync($"/api/v1/family-groups/{group.Id}",
            new UpdateFamilyGroupRequest("tentativa", null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /family-groups/join ──────────────────────────────────────────────

    [Fact]
    public async Task Join_WithValidInviteCode_Returns201()
    {
        var tokenA = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA, "grupo-convite");

        var tokenB = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.PostAsJsonAsync("/api/v1/family-groups/join",
            new JoinFamilyGroupRequest(group.InviteCode));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<FamilyGroupSummaryResponse>(JsonOptions);
        body!.Id.Should().Be(group.Id);
    }

    [Fact]
    public async Task Join_WithInvalidInviteCode_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/family-groups/join",
            new JoinFamilyGroupRequest("CODIGO-INVALIDO"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Join_AlreadyMember_Returns409()
    {
        var token = await AuthenticateAsync();
        var group = await CreateGroupAsync(token, "grupo-membro");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/family-groups/join",
            new JoinFamilyGroupRequest(group.InviteCode));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── POST /family-groups/{id}/leave ────────────────────────────────────────

    [Fact]
    public async Task Leave_AsMember_Returns204()
    {
        var tokenA = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA, "grupo-saida");

        var tokenB = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.PostAsync($"/api/v1/family-groups/{group.Id}/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Leave_AsOwner_Returns409()
    {
        var token = await AuthenticateAsync();
        var group = await CreateGroupAsync(token, "grupo-owner");

        var response = await _client.PostAsync($"/api/v1/family-groups/{group.Id}/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Leave_WhenNotMember_Returns404()
    {
        var tokenA = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA, "grupo-externo");

        var tokenB = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.PostAsync($"/api/v1/family-groups/{group.Id}/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /family-groups/{id}/members/{memberUserId} ─────────────────────

    [Fact]
    public async Task RemoveMember_AsOwner_Returns204()
    {
        var tokenA = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA, "grupo-remocao");

        var tokenB = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var groupDetails = await _client.GetAsync($"/api/v1/family-groups/{group.Id}");
        var details = await groupDetails.Content.ReadFromJsonAsync<FamilyGroupResponse>(JsonOptions);
        var memberB = details!.Members.First(m => m.Role == "Member");

        var response = await _client.DeleteAsync($"/api/v1/family-groups/{group.Id}/members/{memberB.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveMember_AsMember_Returns403()
    {
        var tokenA = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA, "grupo-membro-remover");

        var tokenB = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        var tokenC = await AuthenticateAsync();
        await JoinGroupAsync(tokenC, group.InviteCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var groupDetails = await _client.GetAsync($"/api/v1/family-groups/{group.Id}");
        var details = await groupDetails.Content.ReadFromJsonAsync<FamilyGroupResponse>(JsonOptions);
        var memberC = details!.Members.First(m => m.Role == "Member");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.DeleteAsync($"/api/v1/family-groups/{group.Id}/members/{memberC.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /family-groups/{id}/invite/regenerate ────────────────────────────

    [Fact]
    public async Task RegenerateInviteCode_AsOwner_Returns200()
    {
        var token = await AuthenticateAsync();
        var group = await CreateGroupAsync(token, "grupo-regenerar");
        var oldCode = group.InviteCode;

        var response = await _client.PostAsync($"/api/v1/family-groups/{group.Id}/invite/regenerate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FamilyGroupResponse>(JsonOptions);
        body!.InviteCode.Should().NotBe(oldCode);
    }

    [Fact]
    public async Task RegenerateInviteCode_AsMember_Returns403()
    {
        var tokenA = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA, "grupo-regen-protegido");

        var tokenB = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.PostAsync($"/api/v1/family-groups/{group.Id}/invite/regenerate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── DELETE /family-groups/{id} ────────────────────────────────────────────

    [Fact]
    public async Task Delete_AsOwner_Returns204()
    {
        var token = await AuthenticateAsync();
        var group = await CreateGroupAsync(token, "grupo-excluir");

        var response = await _client.DeleteAsync($"/api/v1/family-groups/{group.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_AsMember_Returns403()
    {
        var tokenA = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA, "grupo-del-protegido");

        var tokenB = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.DeleteAsync($"/api/v1/family-groups/{group.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
