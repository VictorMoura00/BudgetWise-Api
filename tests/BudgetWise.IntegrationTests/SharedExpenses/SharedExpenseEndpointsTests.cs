using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.FamilyGroups.DTOs;
using BudgetWise.Application.SharedExpenses.DTOs;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.SharedExpenses;

public sealed class SharedExpenseEndpointsTests(BudgetWiseWebFactory factory)
    : IClassFixture<BudgetWiseWebFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();
    private readonly Faker _faker = new("pt_BR");

    private async Task<(string Token, Guid UserId)> AuthenticateAsync()
    {
        var email = _faker.Internet.Email();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(_faker.Name.FullName(), email, "Senha@123", "Senha@123"));
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        var userId = GetUserIdFromToken(tokens!.AccessToken);
        return (tokens.AccessToken, userId);
    }

    private static Guid GetUserIdFromToken(string token)
    {
        var payload = token.Split('.')[1];
        var base64 = payload.Replace('-', '+').Replace('_', '/');
        var padded = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        using var doc = JsonDocument.Parse(json);
        var sub = doc.RootElement.GetProperty("sub").GetString()!;
        return Guid.Parse(sub);
    }

    private async Task<FamilyGroupResponse> CreateGroupAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/family-groups",
            new CreateFamilyGroupRequest(_faker.Commerce.Department(), _faker.Lorem.Sentence()));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FamilyGroupResponse>(JsonOptions))!;
    }

    private async Task<SharedExpenseResponse> CreateSharedExpenseAsync(
        string token,
        Guid groupId,
        Guid participantUserId,
        decimal total = 200m,
        decimal participantShare = 100m)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new CreateSharedExpenseRequest(
            "Conta de luz",
            total,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            [new ParticipantRequest(participantUserId, participantShare)]);
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/family-groups/{groupId}/shared-expenses", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SharedExpenseResponse>(JsonOptions))!;
    }

    // ── POST /family-groups/{id}/shared-expenses ──────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_Returns201()
    {
        var (token, userId) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);
        var (tokenB, userBId) = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new CreateSharedExpenseRequest(
            "Churrasco de domingo",
            300m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            [new ParticipantRequest(userBId, 150m)]);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SharedExpenseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Description.Should().Be("Churrasco de domingo");
        body.TotalAmount.Should().Be(300m);
        body.CreatedBy.Should().Be(userId);
        body.Participants.Should().HaveCount(1);
        body.Participants[0].UserId.Should().Be(userBId);
        body.Participants[0].IsSettled.Should().BeFalse();
        body.TotalPending.Should().Be(150m);
        body.IsFullySettled.Should().BeFalse();
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/family-groups/{Guid.NewGuid()}/shared-expenses",
            new CreateSharedExpenseRequest("test", 100m, DateOnly.FromDateTime(DateTime.UtcNow), null,
                [new ParticipantRequest(Guid.NewGuid(), 100m)]));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WhenNotMember_Returns403Or404()
    {
        var (tokenA, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA);

        var (tokenB, _) = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var request = new CreateSharedExpenseRequest(
            "Despesa",
            100m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            [new ParticipantRequest(Guid.NewGuid(), 100m)]);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithMissingDescription_Returns400()
    {
        var (token, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new CreateSharedExpenseRequest(
            "",
            100m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            [new ParticipantRequest(Guid.NewGuid(), 100m)]);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /family-groups/{id}/shared-expenses ───────────────────────────────

    [Fact]
    public async Task GetList_AsMember_Returns200WithExpenses()
    {
        var (token, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);
        var (tokenB, userBId) = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        await CreateSharedExpenseAsync(token, group.Id, userBId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync($"/api/v1/family-groups/{group.Id}/shared-expenses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<SharedExpenseResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetList_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(
            $"/api/v1/family-groups/{Guid.NewGuid()}/shared-expenses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /family-groups/{id}/shared-expenses/{expenseId} ───────────────────

    [Fact]
    public async Task GetById_WhenExists_Returns200()
    {
        var (token, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);
        var (tokenB, userBId) = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        var expense = await CreateSharedExpenseAsync(token, group.Id, userBId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses/{expense.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SharedExpenseResponse>(JsonOptions);
        body!.Id.Should().Be(expense.Id);
        body.Participants.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var (token, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /{id}/participants/{userId}/settle ────────────────────────────────

    [Fact]
    public async Task Settle_ValidParticipant_Returns200WithSettledState()
    {
        var (token, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);
        var (tokenB, userBId) = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        var expense = await CreateSharedExpenseAsync(token, group.Id, userBId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses/{expense.Id}/participants/{userBId}/settle",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SharedExpenseResponse>(JsonOptions);
        body!.Participants.Should().ContainSingle(p => p.UserId == userBId && p.IsSettled);
        body.TotalSettled.Should().Be(100m);
        body.TotalPending.Should().Be(0m);
        body.IsFullySettled.Should().BeTrue();
    }

    [Fact]
    public async Task Settle_WhenAlreadySettled_Returns422()
    {
        var (token, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);
        var (tokenB, userBId) = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        var expense = await CreateSharedExpenseAsync(token, group.Id, userBId);
        var settleUrl =
            $"/api/v1/family-groups/{group.Id}/shared-expenses/{expense.Id}/participants/{userBId}/settle";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsync(settleUrl, null);
        var response = await _client.PostAsync(settleUrl, null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Settle_WhenParticipantNotFound_Returns404()
    {
        var (token, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);
        var (tokenB, userBId) = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        var expense = await CreateSharedExpenseAsync(token, group.Id, userBId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses/{expense.Id}/participants/{Guid.NewGuid()}/settle",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /family-groups/{id}/shared-expenses/summary ──────────────────────

    [Fact]
    public async Task GetSummary_WithNoExpenses_ReturnsZeroedSummary()
    {
        var (token, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SharedExpenseSummaryResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.TotalExpenses.Should().Be(0);
        body.TotalAmount.Should().Be(0m);
        body.TotalSettled.Should().Be(0m);
        body.TotalPending.Should().Be(0m);
        body.FullySettledCount.Should().Be(0);
        body.PartiallySettledCount.Should().Be(0);
        body.UnsettledCount.Should().Be(0);
        body.ParticipantTotals.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSummary_WithExpenses_ReturnsCorrectedTotals()
    {
        var (token, userId) = await AuthenticateAsync();
        var group = await CreateGroupAsync(token);
        var (tokenB, userBId) = await AuthenticateAsync();
        await JoinGroupAsync(tokenB, group.InviteCode);

        // Create two expenses: one partially settled
        var expense = await CreateSharedExpenseAsync(token, group.Id, userBId, total: 200m, participantShare: 200m);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses/{expense.Id}/participants/{userBId}/settle",
            null);

        await CreateSharedExpenseAsync(token, group.Id, userBId, total: 100m, participantShare: 100m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SharedExpenseSummaryResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.TotalExpenses.Should().Be(2);
        body.TotalAmount.Should().Be(300m);
        body.TotalSettled.Should().Be(200m);
        body.TotalPending.Should().Be(100m);
        body.FullySettledCount.Should().Be(1);
        body.UnsettledCount.Should().Be(1);
        body.ParticipantTotals.Should().ContainSingle(p => p.UserId == userBId);

        var participant = body.ParticipantTotals.Single(p => p.UserId == userBId);
        participant.AmountOwed.Should().Be(300m);
        participant.AmountSettled.Should().Be(200m);
        participant.AmountPending.Should().Be(100m);
        participant.UserName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetSummary_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(
            $"/api/v1/family-groups/{Guid.NewGuid()}/shared-expenses/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_WhenNotMember_Returns403Or404()
    {
        var (tokenA, _) = await AuthenticateAsync();
        var group = await CreateGroupAsync(tokenA);

        var (tokenB, _) = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await _client.GetAsync(
            $"/api/v1/family-groups/{group.Id}/shared-expenses/summary");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    private async Task JoinGroupAsync(string token, string inviteCode)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/family-groups/join",
            new JoinFamilyGroupRequest(inviteCode));
        response.EnsureSuccessStatusCode();
    }
}
