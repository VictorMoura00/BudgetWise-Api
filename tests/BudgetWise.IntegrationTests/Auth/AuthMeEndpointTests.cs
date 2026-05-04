using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.Auth;

public sealed class AuthMeEndpointTests(BudgetWiseWebFactory factory)
    : IClassFixture<BudgetWiseWebFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();
    private readonly Faker _faker = new("pt_BR");

    private async Task<(string Token, string Email, string FullName)> AuthenticateAsync()
    {
        var fullName = _faker.Name.FullName();
        var email = _faker.Internet.Email();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(fullName, email, "Senha@123", "Senha@123"));
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
        return (auth.AccessToken, email, fullName);
    }

    [Fact]
    public async Task GetMe_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsCurrentUser()
    {
        var (token, email, fullName) = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Email.Should().Be(email);
        body.FullName.Should().Be(fullName);
        body.IsActive.Should().BeTrue();
        body.UserId.Should().NotBeEmpty();
        body.Role.Should().Be("User");
    }

    [Fact]
    public async Task GetMe_TwoDifferentUsers_ReturnCorrectUser()
    {
        var (tokenA, emailA, _) = await AuthenticateAsync();
        var (tokenB, emailB, _) = await AuthenticateAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var responseA = await _client.GetAsync("/api/v1/auth/me");
        var bodyA = await responseA.Content.ReadFromJsonAsync<CurrentUserResponse>(JsonOptions);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var responseB = await _client.GetAsync("/api/v1/auth/me");
        var bodyB = await responseB.Content.ReadFromJsonAsync<CurrentUserResponse>(JsonOptions);

        bodyA!.Email.Should().Be(emailA);
        bodyB!.Email.Should().Be(emailB);
        bodyA.UserId.Should().NotBe(bodyB.UserId);
    }
}
