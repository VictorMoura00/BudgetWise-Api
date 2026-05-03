using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Reports.DTOs;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Enums;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.Reports;

public sealed class DueTransactionsReportEndpointsTests(BudgetWiseWebFactory factory)
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
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!.AccessToken;
    }

    private async Task CreateTransactionAsync(string token, DateOnly? dueDate, bool isConfirmed = false)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new CreateTransactionRequest(
            Description: _faker.Commerce.ProductName(),
            Amount: 100m,
            Type: TransactionType.Expense,
            TransactionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            CategoryId: null,
            Notes: null,
            RecurrenceType: RecurrenceType.None,
            RecurrenceEndDate: null,
            IsConfirmed: isConfirmed,
            PaymentMethod: PaymentMethod.Pix,
            FamilyGroupId: null,
            DueDate: dueDate);

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", request);
        response.EnsureSuccessStatusCode();
    }

    // ── GET /reports/due-transactions ─────────────────────────────────────────

    [Fact]
    public async Task GetDueTransactions_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDueTransactions_WithNoTransactions_ReturnsEmptyGroupsAndZeroTotals()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.TotalPendingCount.Should().Be(0);
        body.TotalPendingAmount.Should().Be(0m);
        body.Overdue.Count.Should().Be(0);
        body.DueToday.Count.Should().Be(0);
        body.DueNext7Days.Count.Should().Be(0);
        body.DueNext30Days.Count.Should().Be(0);
        body.Future.Count.Should().Be(0);
        body.WithoutDueDate.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetDueTransactions_WithOverdueTransaction_AppearsInOverdueGroup()
    {
        var token = await AuthenticateAsync();
        var pastDue = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);
        await CreateTransactionAsync(token, pastDue);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.Overdue.Count.Should().BeGreaterThanOrEqualTo(1);
        body.Overdue.TotalAmount.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task GetDueTransactions_WithDueTodayTransaction_AppearsInDueTodayGroup()
    {
        var token = await AuthenticateAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await CreateTransactionAsync(token, today);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.DueToday.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDueTransactions_WithDueNext7DaysTransaction_AppearsInCorrectGroup()
    {
        var token = await AuthenticateAsync();
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
        await CreateTransactionAsync(token, dueDate);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.DueNext7Days.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDueTransactions_WithFutureTransaction_AppearsInFutureGroup()
    {
        var token = await AuthenticateAsync();
        var futureDue = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(45);
        await CreateTransactionAsync(token, futureDue);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.Future.Count.Should().BeGreaterThanOrEqualTo(1);
        body.Future.TotalAmount.Should().BeGreaterThan(0m);
        body.DueNext30Days.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetDueTransactions_WithNoDueDate_AppearsInWithoutDueDateGroup()
    {
        var token = await AuthenticateAsync();
        await CreateTransactionAsync(token, dueDate: null);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.WithoutDueDate.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDueTransactions_TotalPendingCount_AndAmount_SumAllGroups()
    {
        var token = await AuthenticateAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateTransactionAsync(token, today.AddDays(-1));      // Overdue
        await CreateTransactionAsync(token, today.AddDays(3));       // DueNext7Days
        await CreateTransactionAsync(token, today.AddDays(60));      // Future
        await CreateTransactionAsync(token, null);                   // WithoutDueDate

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.TotalPendingCount.Should().BeGreaterThanOrEqualTo(4);
        body.TotalPendingAmount.Should().BeGreaterThanOrEqualTo(400m);
    }

    [Fact]
    public async Task GetDueTransactions_ConfirmedTransactions_AreExcluded()
    {
        var token = await AuthenticateAsync();
        var pastDue = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        await CreateTransactionAsync(token, pastDue, isConfirmed: true);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.Overdue.Count.Should().Be(0);
        body.TotalPendingCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDueTransactions_WithIncludeItemsTrue_ReturnsTransactionDetails()
    {
        var token = await AuthenticateAsync();
        var pastDue = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        await CreateTransactionAsync(token, pastDue);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions?includeItems=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.Overdue.Items.Should().NotBeEmpty();
        body.Overdue.Items[0].Amount.Should().Be(100m);
        body.Overdue.Items[0].DueDate.Should().Be(pastDue);
    }

    [Fact]
    public async Task GetDueTransactions_WithIncludeItemsTrue_FutureGroupHasItems()
    {
        var token = await AuthenticateAsync();
        var futureDue = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);
        await CreateTransactionAsync(token, futureDue);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions?includeItems=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.Future.Items.Should().NotBeEmpty();
        body.Future.Items[0].Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetDueTransactions_WithIncludeItemsFalse_ItemsAreEmpty()
    {
        var token = await AuthenticateAsync();
        await CreateTransactionAsync(token, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
        await CreateTransactionAsync(token, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions?includeItems=false");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.Overdue.Items.Should().BeEmpty();
        body.Future.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDueTransactions_IsolatedByUser_OtherUserTransactionsNotVisible()
    {
        var tokenA = await AuthenticateAsync();
        var tokenB = await AuthenticateAsync();

        await CreateTransactionAsync(tokenA, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
        await CreateTransactionAsync(tokenA, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.GetAsync("/api/v1/reports/due-transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DueTransactionsReportResponse>(JsonOptions);
        body!.TotalPendingCount.Should().Be(0);
        body.Overdue.Count.Should().Be(0);
        body.Future.Count.Should().Be(0);
    }
}
