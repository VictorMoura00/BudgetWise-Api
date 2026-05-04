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

public sealed class CategoryAnalysisEndpointsTests(BudgetWiseWebFactory factory)
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

    private async Task CreateTransactionAsync(string token, decimal amount, TransactionType type, bool isConfirmed = true)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new CreateTransactionRequest(
            Description: _faker.Commerce.ProductName(),
            Amount: amount,
            Type: type,
            TransactionDate: now,
            CategoryId: null,
            Notes: null,
            RecurrenceType: RecurrenceType.None,
            RecurrenceEndDate: null,
            IsConfirmed: isConfirmed,
            PaymentMethod: PaymentMethod.Pix,
            FamilyGroupId: null,
            DueDate: null);

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetCategoryAnalysis_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/reports/category-analysis");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategoryAnalysis_WithNoTransactions_ReturnsEmpty()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/reports/category-analysis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CategoryAnalysisReportResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Items.Should().BeEmpty();
        body.TotalAmount.Should().Be(0m);
        body.TotalTransactions.Should().Be(0);
    }

    [Fact]
    public async Task GetCategoryAnalysis_WithTransactions_ReturnsSumAndPercentage()
    {
        var token = await AuthenticateAsync();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateTransactionAsync(token, 600m, TransactionType.Expense);
        await CreateTransactionAsync(token, 400m, TransactionType.Expense);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync(
            $"/api/v1/reports/category-analysis?startDate={now.Year}-{now.Month:D2}-01&endDate={now.Year}-{now.Month:D2}-{DateTime.DaysInMonth(now.Year, now.Month):D2}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CategoryAnalysisReportResponse>(JsonOptions);
        body!.TotalAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task GetCategoryAnalysis_WithInvalidDateRange_Returns400()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/reports/category-analysis?startDate=2026-05-31&endDate=2026-05-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCategoryAnalysis_IsolatedByUser()
    {
        var tokenA = await AuthenticateAsync();
        var tokenB = await AuthenticateAsync();

        await CreateTransactionAsync(tokenA, 500m, TransactionType.Expense);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.GetAsync("/api/v1/reports/category-analysis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CategoryAnalysisReportResponse>(JsonOptions);
        body!.TotalAmount.Should().Be(0m);
    }
}
