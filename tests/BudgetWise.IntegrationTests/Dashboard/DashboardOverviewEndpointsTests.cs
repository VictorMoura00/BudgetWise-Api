using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Dashboard.DTOs;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Enums;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.Dashboard;

public sealed class DashboardOverviewEndpointsTests(BudgetWiseWebFactory factory)
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

    private async Task CreateTransactionAsync(
        string token,
        decimal amount,
        TransactionType type,
        bool isConfirmed = false,
        DateOnly? dueDate = null,
        DateOnly? transactionDate = null)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var date = transactionDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new CreateTransactionRequest(
            Description: _faker.Commerce.ProductName(),
            Amount: amount,
            Type: type,
            TransactionDate: date,
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

    // ── GET /dashboard/overview ──────────────────────────────────────────────

    [Fact]
    public async Task GetOverview_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/dashboard/overview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOverview_WithNoTransactions_ReturnsZeroedSummary()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/dashboard/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardOverviewResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.FinancialSummary.TotalIncome.Should().Be(0m);
        body.FinancialSummary.TotalExpense.Should().Be(0m);
        body.FinancialSummary.Balance.Should().Be(0m);
        body.FinancialSummary.SavingsRate.Should().BeNull();
        body.PendingSummary.TotalPendingCount.Should().Be(0);
        body.PendingSummary.TotalPendingAmount.Should().Be(0m);
        body.LargestExpense.Should().BeNull();
        body.CategoryHighlights.TopExpenseCategory.Should().BeNull();
        body.CategoryHighlights.TopIncomeCategory.Should().BeNull();
    }

    [Fact]
    public async Task GetOverview_UsesPeriodFromQueryParams()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/dashboard/overview?year=2024&month=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardOverviewResponse>(JsonOptions);
        body!.Period.Year.Should().Be(2024);
        body.Period.Month.Should().Be(3);
        body.Period.StartDate.Should().Be(new DateOnly(2024, 3, 1));
        body.Period.EndDate.Should().Be(new DateOnly(2024, 3, 31));
    }

    [Fact]
    public async Task GetOverview_DefaultsToCurrentMonth_WhenNoQueryParams()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var now = DateTime.UtcNow;

        var response = await _client.GetAsync("/api/v1/dashboard/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardOverviewResponse>(JsonOptions);
        body!.Period.Year.Should().Be(now.Year);
        body.Period.Month.Should().Be(now.Month);
    }

    [Fact]
    public async Task GetOverview_WithIncomeAndExpense_ComputesCorrectFinancialSummary()
    {
        var token = await AuthenticateAsync();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateTransactionAsync(token, 1000m, TransactionType.Income, isConfirmed: true, transactionDate: now);
        await CreateTransactionAsync(token, 400m, TransactionType.Expense, isConfirmed: true, transactionDate: now);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync($"/api/v1/dashboard/overview?year={now.Year}&month={now.Month}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardOverviewResponse>(JsonOptions);
        body!.FinancialSummary.TotalIncome.Should().Be(1000m);
        body.FinancialSummary.TotalExpense.Should().Be(400m);
        body.FinancialSummary.Balance.Should().Be(600m);
        body.FinancialSummary.SavingsRate.Should().Be(60m);
    }

    [Fact]
    public async Task GetOverview_PendingTransactions_AppearInPendingSummary()
    {
        var token = await AuthenticateAsync();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateTransactionAsync(token, 100m, TransactionType.Expense, isConfirmed: false, dueDate: now.AddDays(-1));
        await CreateTransactionAsync(token, 200m, TransactionType.Expense, isConfirmed: false, dueDate: now.AddDays(5));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/dashboard/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardOverviewResponse>(JsonOptions);
        body!.PendingSummary.TotalPendingCount.Should().BeGreaterThanOrEqualTo(2);
        body.PendingSummary.Overdue.Count.Should().BeGreaterThanOrEqualTo(1);
        body.PendingSummary.DueNext7Days.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetOverview_ConfirmedTransactions_ExcludedFromPendingSummary()
    {
        var token = await AuthenticateAsync();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateTransactionAsync(token, 500m, TransactionType.Expense, isConfirmed: true, dueDate: now.AddDays(-2));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/dashboard/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardOverviewResponse>(JsonOptions);
        body!.PendingSummary.Overdue.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetOverview_LargestExpense_IsReturnedForCurrentPeriod()
    {
        var token = await AuthenticateAsync();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateTransactionAsync(token, 50m, TransactionType.Expense, isConfirmed: true, transactionDate: now);
        await CreateTransactionAsync(token, 999m, TransactionType.Expense, isConfirmed: true, transactionDate: now);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync($"/api/v1/dashboard/overview?year={now.Year}&month={now.Month}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardOverviewResponse>(JsonOptions);
        body!.LargestExpense.Should().NotBeNull();
        body.LargestExpense!.Amount.Should().Be(999m);
    }

    [Fact]
    public async Task GetOverview_IsolatedByUser_OtherUserTransactionsNotVisible()
    {
        var tokenA = await AuthenticateAsync();
        var tokenB = await AuthenticateAsync();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateTransactionAsync(tokenA, 1000m, TransactionType.Income, isConfirmed: true, transactionDate: now);
        await CreateTransactionAsync(tokenA, 500m, TransactionType.Expense, isConfirmed: true, transactionDate: now);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.GetAsync($"/api/v1/dashboard/overview?year={now.Year}&month={now.Month}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardOverviewResponse>(JsonOptions);
        body!.FinancialSummary.TotalIncome.Should().Be(0m);
        body.FinancialSummary.TotalExpense.Should().Be(0m);
        body.LargestExpense.Should().BeNull();
    }

    [Fact]
    public async Task GetOverview_MonthlyComparison_HasPreviousMonthData()
    {
        var token = await AuthenticateAsync();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var prevMonth = now.AddMonths(-1);

        await CreateTransactionAsync(token, 800m, TransactionType.Income, isConfirmed: true, transactionDate: prevMonth);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync($"/api/v1/dashboard/overview?year={now.Year}&month={now.Month}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardOverviewResponse>(JsonOptions);
        body!.MonthlyComparison.PreviousIncome.Should().Be(800m);
    }
}
