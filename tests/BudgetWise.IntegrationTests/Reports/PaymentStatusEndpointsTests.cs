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

public sealed class PaymentStatusEndpointsTests(BudgetWiseWebFactory factory)
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
        bool isConfirmed,
        DateOnly? dueDate = null)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new CreateTransactionRequest(
            Description: _faker.Commerce.ProductName(),
            Amount: amount,
            Type: TransactionType.Expense,
            TransactionDate: now,
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

    [Fact]
    public async Task GetPaymentStatus_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/reports/payment-status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPaymentStatus_WithNoTransactions_ReturnsZeroedGroups()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/reports/payment-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusReportResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Confirmed.Count.Should().Be(0);
        body.Pending.Count.Should().Be(0);
        body.Overdue.Count.Should().Be(0);
        body.DueToday.Count.Should().Be(0);
        body.DueNext7Days.Count.Should().Be(0);
        body.WithoutDueDate.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetPaymentStatus_ConfirmedTransactions_AppearInConfirmedGroup()
    {
        var token = await AuthenticateAsync();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateTransactionAsync(token, 300m, isConfirmed: true);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/payment-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusReportResponse>(JsonOptions);
        body!.Confirmed.Count.Should().BeGreaterThanOrEqualTo(1);
        body.Confirmed.TotalAmount.Should().BeGreaterThanOrEqualTo(300m);
    }

    [Fact]
    public async Task GetPaymentStatus_OverdueTransactions_AppearInOverdueGroup()
    {
        var token = await AuthenticateAsync();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateTransactionAsync(token, 150m, isConfirmed: false, dueDate: now.AddDays(-5));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/reports/payment-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusReportResponse>(JsonOptions);
        body!.Overdue.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetPaymentStatus_WithInvalidDateRange_Returns400()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/reports/payment-status?startDate=2026-05-31&endDate=2026-05-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPaymentStatus_IsolatedByUser()
    {
        var tokenA = await AuthenticateAsync();
        var tokenB = await AuthenticateAsync();

        await CreateTransactionAsync(tokenA, 999m, isConfirmed: true);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.GetAsync("/api/v1/reports/payment-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusReportResponse>(JsonOptions);
        body!.Confirmed.Count.Should().Be(0);
    }
}
