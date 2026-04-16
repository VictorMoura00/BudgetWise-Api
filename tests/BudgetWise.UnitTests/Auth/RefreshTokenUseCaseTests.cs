using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.UseCases;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace BudgetWise.UnitTests.Auth;

public sealed class RefreshTokenUseCaseTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly RefreshTokenUseCase _sut;

    public RefreshTokenUseCaseTests()
    {
        _configuration["Jwt:RefreshTokenExpirationDays"].Returns("7");
        _tokenService.GenerateAccessToken(default, default!, default!, default).ReturnsForAnyArgs("new-access-token");
        _tokenService.GenerateRefreshToken().Returns("new-refresh-token");

        _sut = new RefreshTokenUseCase(_authService, _tokenService, _configuration);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRefreshToken_ReturnsNewTokenPair()
    {
        // Arrange
        var request = AuthFaker.ValidRefreshTokenRequest();
        var userDto = AuthFaker.AuthUserDto();
        _authService.ValidateRefreshTokenAsync(default, default!).ReturnsForAnyArgs(userDto);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidRefreshToken_ReturnsFailure()
    {
        // Arrange
        var request = AuthFaker.ValidRefreshTokenRequest();
        _authService.ValidateRefreshTokenAsync(default, default!)
            .ReturnsForAnyArgs(AuthErrors.InvalidRefreshToken);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task ExecuteAsync_WhenValid_StoresNewRefreshToken()
    {
        // Arrange
        var request = AuthFaker.ValidRefreshTokenRequest();
        var userDto = AuthFaker.AuthUserDto();
        _authService.ValidateRefreshTokenAsync(default, default!).ReturnsForAnyArgs(userDto);

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        await _authService.Received(1).StoreRefreshTokenAsync(
            userDto.Id, "new-refresh-token", 7, Arg.Any<CancellationToken>());
    }
}
