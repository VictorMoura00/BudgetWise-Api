using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.Login;
using BudgetWise.Application.Interfaces;
using BudgetWise.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace BudgetWise.UnitTests.Auth;

public sealed class LoginUserUseCaseTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly LoginUserUseCase _sut;

    public LoginUserUseCaseTests()
    {
        _configuration["Jwt:RefreshTokenExpirationDays"].Returns("7");
        _tokenService.GenerateAccessToken(default, default!, default!).ReturnsForAnyArgs("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");

        _sut = new LoginUserUseCase(_authService, _tokenService, _configuration);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var request = AuthFaker.ValidLoginRequest();
        var userDto = AuthFaker.AuthUserDto();
        _authService.ValidateCredentialsAsync(default!, default!).ReturnsForAnyArgs(userDto);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var request = AuthFaker.ValidLoginRequest();
        _authService.ValidateCredentialsAsync(default!, default!)
            .ReturnsForAnyArgs(AuthErrors.InvalidCredentials);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledAccount_ReturnsFailure()
    {
        // Arrange
        var request = AuthFaker.ValidLoginRequest();
        _authService.ValidateCredentialsAsync(default!, default!)
            .ReturnsForAnyArgs(AuthErrors.AccountDisabled);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.AccountDisabled");
    }

    [Fact]
    public async Task ExecuteAsync_WithLockedAccount_ReturnsFailure()
    {
        // Arrange
        var request = AuthFaker.ValidLoginRequest();
        _authService.ValidateCredentialsAsync(default!, default!)
            .ReturnsForAnyArgs(AuthErrors.AccountLockedOut);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.AccountLockedOut");
    }

    [Fact]
    public async Task ExecuteAsync_WhenLoginSucceeds_StoresRefreshToken()
    {
        // Arrange
        var request = AuthFaker.ValidLoginRequest();
        var userDto = AuthFaker.AuthUserDto();
        _authService.ValidateCredentialsAsync(default!, default!).ReturnsForAnyArgs(userDto);

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        await _authService.Received(1).StoreRefreshTokenAsync(
            userDto.Id, "refresh-token", 7, Arg.Any<CancellationToken>());
    }
}
