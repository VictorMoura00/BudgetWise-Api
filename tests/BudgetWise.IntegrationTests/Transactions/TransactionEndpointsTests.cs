using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Categories.DTOs;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Enums;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.Transactions;

public sealed class TransactionEndpointsTests(BudgetWiseWebFactory factory)
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

    private CreateTransactionRequest ValidCreateRequest(
        TransactionType type = TransactionType.Expense,
        bool isConfirmed = false) => new(
            Description: _faker.Commerce.ProductName(),
            Amount: _faker.Random.Decimal(10, 5000),
            Type: type,
            TransactionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            CategoryId: null,
            Notes: null,
            RecurrenceType: RecurrenceType.None,
            RecurrenceEndDate: null,
            IsConfirmed: isConfirmed,
            PaymentMethod: PaymentMethod.Pix,
            FamilyGroupId: null);

    private async Task<CategoryResponse> CreateCategoryAsync(string token, CategoryType categoryType)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/categories",
            new CreateCategoryRequest(_faker.Commerce.Department(), null, null, null, categoryType));
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions))!;
    }

    // ── POST /transactions ────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_Returns201()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.IsConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invalidRequest = new CreateTransactionRequest(
            Description: "",
            Amount: -100m,
            Type: TransactionType.Expense,
            TransactionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            CategoryId: null, Notes: null,
            RecurrenceType: RecurrenceType.None,
            RecurrenceEndDate: null, IsConfirmed: false,
            PaymentMethod: null, FamilyGroupId: null);

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", invalidRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /transactions ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetList_WithoutPaginationParams_Returns200WithDefaults()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest());
        await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest(TransactionType.Income));

        var response = await _client.GetAsync("/api/v1/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedTransactionResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        body.PageNumber.Should().Be(1);
        body.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetList_WithTypeFilter_ReturnsFilteredResults()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest(TransactionType.Income));
        await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest(TransactionType.Expense));

        var response = await _client.GetAsync("/api/v1/transactions?pageNumber=1&pageSize=20&type=Income");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedTransactionResponse>(JsonOptions);
        body!.Items.Should().AllSatisfy(t => t.Type.Should().Be(TransactionType.Income));
    }

    [Fact]
    public async Task GetList_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/transactions?pageNumber=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /transactions/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenExists_Returns200()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var response = await _client.GetAsync($"/api/v1/transactions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        body!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/transactions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WhenBelongsToAnotherUser_Returns404()
    {
        var tokenA = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var tokenB = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.GetAsync($"/api/v1/transactions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /transactions/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithValidData_Returns200()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var updateRequest = new UpdateTransactionRequest(
            "Descrição Atualizada", 999m, TransactionType.Income,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null, "Nota atualizada", RecurrenceType.None, null, PaymentMethod.Cash, null);

        var response = await _client.PutAsJsonAsync($"/api/v1/transactions/{created!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        body!.Description.Should().Be("Descrição Atualizada");
        body.Amount.Should().Be(999m);
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateTransactionRequest(
            "Test", 100m, TransactionType.Expense,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null, null, RecurrenceType.None, null, null, null);

        var response = await _client.PutAsJsonAsync($"/api/v1/transactions/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /transactions/{id}/confirm ──────────────────────────────────────

    [Fact]
    public async Task Confirm_WhenPending_Returns200WithIsConfirmedTrueAndPaidAt()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions",
            ValidCreateRequest(isConfirmed: false));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var response = await _client.PatchAsync($"/api/v1/transactions/{created!.Id}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        body!.IsConfirmed.Should().BeTrue();
        body.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Confirm_WithExplicitPaidAt_ReturnsProvidedDate()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions",
            ValidCreateRequest(isConfirmed: false));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var dataPagamento = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);
        var confirmRequest = new { PaidAt = dataPagamento.ToString("yyyy-MM-dd") };
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/transactions/{created!.Id}/confirm", confirmRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        body!.PaidAt.Should().Be(dataPagamento);
    }

    [Fact]
    public async Task Confirm_WhenAlreadyConfirmed_Returns400()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions",
            ValidCreateRequest(isConfirmed: true));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var response = await _client.PatchAsync($"/api/v1/transactions/{created!.Id}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── DELETE /transactions/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenExists_Returns204()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var response = await _client.DeleteAsync($"/api/v1/transactions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WhenAlreadyDeleted_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        await _client.DeleteAsync($"/api/v1/transactions/{created!.Id}");
        var response = await _client.DeleteAsync($"/api/v1/transactions/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync($"/api/v1/transactions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Category compatibility ────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithIncompatibleCategory_Returns400()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var incomeCategory = await CreateCategoryAsync(token, CategoryType.Income);

        var request = new CreateTransactionRequest(
            _faker.Commerce.ProductName(), 100m, TransactionType.Expense,
            DateOnly.FromDateTime(DateTime.UtcNow),
            incomeCategory.Id, null, RecurrenceType.None, null, false, null, null);

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithCompatibleBothCategory_Returns201()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bothCategory = await CreateCategoryAsync(token, CategoryType.Both);

        var request = new CreateTransactionRequest(
            _faker.Commerce.ProductName(), 200m, TransactionType.Expense,
            DateOnly.FromDateTime(DateTime.UtcNow),
            bothCategory.Id, null, RecurrenceType.None, null, false, null, null);

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Update_WithIncompatibleCategory_Returns400()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", ValidCreateRequest(TransactionType.Income));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var expenseCategory = await CreateCategoryAsync(token, CategoryType.Expense);

        var updateRequest = new UpdateTransactionRequest(
            created!.Description, created.Amount, TransactionType.Income,
            created.TransactionDate,
            expenseCategory.Id, null, RecurrenceType.None, null, null, null);

        var response = await _client.PutAsJsonAsync($"/api/v1/transactions/{created.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
