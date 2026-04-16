using System.Net;
using System.Net.Http.Json;
using Bogus;
using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.Auth;

public sealed class AuthEndpointsTests(BudgetWiseWebFactory factory)
    : IClassFixture<BudgetWiseWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly Faker _faker = new("pt_BR");

    // ── /register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns201WithTokens()
    {
        // Arrange
        var request = new RegisterUserRequest(
            FullName: _faker.Name.FullName(),
            Email: _faker.Internet.Email(),
            Password: "Senha@123",
            ConfirmPassword: "Senha@123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        // Arrange — registra uma vez
        var request = new RegisterUserRequest(
            FullName: _faker.Name.FullName(),
            Email: _faker.Internet.Email(),
            Password: "Senha@123",
            ConfirmPassword: "Senha@123");

        await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Act — tenta registrar de novo com o mesmo email
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidData_Returns400()
    {
        // Arrange — senha sem caractere especial, confirmação divergente
        var request = new RegisterUserRequest(
            FullName: "",
            Email: "email-invalido",
            Password: "fraca",
            ConfirmPassword: "diferente");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── /login ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        // Arrange — cria um usuário primeiro
        var email = _faker.Internet.Email();
        var password = "Senha@123";

        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterUserRequest(
            FullName: _faker.Name.FullName(),
            Email: email,
            Password: password,
            ConfirmPassword: password));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginUserRequest(email, password));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        // Arrange
        var email = _faker.Internet.Email();
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterUserRequest(
            _faker.Name.FullName(), email, "Senha@123", "Senha@123"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginUserRequest(email, "SenhaErrada@999"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginUserRequest("naoexiste@test.com", "Senha@123"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyBody_Returns400()
    {
        // Arrange
        var request = new LoginUserRequest("", "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_After5FailedAttempts_Returns401WithLockout()
    {
        // Arrange — cria o usuário
        var email = _faker.Internet.Email();
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(_faker.Name.FullName(), email, "Senha@123", "Senha@123"));

        // Faz 5 tentativas com senha errada para ativar o lockout
        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginUserRequest(email, "SenhaErrada@999"));
        }

        // Act — tenta logar com a senha correta, mas conta já está bloqueada
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginUserRequest(email, "Senha@123"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── /refresh ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithValidToken_Returns200WithNewTokens()
    {
        // Arrange — registra e obtém tokens
        var email = _faker.Internet.Email();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(_faker.Name.FullName(), email, "Senha@123", "Senha@123"));

        var tokens = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest(tokens!.UserId, tokens.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newTokens = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newTokens!.AccessToken.Should().NotBe(tokens.AccessToken);
        newTokens.RefreshToken.Should().NotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest(Guid.NewGuid(), "token-invalido"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_UsedTokenCannotBeReused_Returns401()
    {
        // Arrange — obtém tokens
        var email = _faker.Internet.Email();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(_faker.Name.FullName(), email, "Senha@123", "Senha@123"));

        var tokens = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var refreshRequest = new RefreshTokenRequest(tokens!.UserId, tokens.RefreshToken);

        // Usa o refresh token uma vez
        await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Act — tenta reusar o mesmo token
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert — rotação obrigatória: token já foi substituído
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithEmptyBody_Returns400()
    {
        // Arrange
        var request = new RefreshTokenRequest(Guid.Empty, "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithTokenFromAnotherUser_Returns401()
    {
        // Arrange — registra dois usuários distintos
        var emailA = _faker.Internet.Email();
        var emailB = _faker.Internet.Email();

        var responseA = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(_faker.Name.FullName(), emailA, "Senha@123", "Senha@123"));

        var responseB = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(_faker.Name.FullName(), emailB, "Senha@123", "Senha@123"));

        var tokensA = await responseA.Content.ReadFromJsonAsync<AuthResponse>();
        var tokensB = await responseB.Content.ReadFromJsonAsync<AuthResponse>();

        // Act — tenta usar o refresh token de B com o userId de A
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest(tokensA!.UserId, tokensB!.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}