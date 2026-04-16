using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.UseCases;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace BudgetWise.UnitTests.Auth;

public sealed class RegisterUserUseCaseTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly RegisterUserUseCase _sut;

    public RegisterUserUseCaseTests()
    {
        _configuration["Jwt:RefreshTokenExpirationDays"].Returns("7");
        _tokenService.GenerateAccessToken(default, default!, default!, default).ReturnsForAnyArgs("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");

        _sut = new RegisterUserUseCase(_authService, _tokenService, _configuration);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ReturnsAuthResponse()
    {
        // Arrange
        var request = AuthFaker.ValidRegisterRequest();
        var userDto = AuthFaker.AuthUserDto();
        _authService.RegisterAsync(default!, default!, default!).ReturnsForAnyArgs(userDto);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.Email.Should().Be(userDto.Email);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var request = AuthFaker.ValidRegisterRequest();
        _authService.RegisterAsync(default!, default!, default!)
            .ReturnsForAnyArgs(AuthErrors.EmailAlreadyExists);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.EmailAlreadyExists");
    }

    [Fact]
    public async Task ExecuteAsync_WhenRegistrationSucceeds_StoresRefreshToken()
    {
        // Arrange
        var request = AuthFaker.ValidRegisterRequest();
        var userDto = AuthFaker.AuthUserDto();
        _authService.RegisterAsync(default!, default!, default!).ReturnsForAnyArgs(userDto);

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        await _authService.Received(1).StoreRefreshTokenAsync(
            userDto.Id,
            "refresh-token",
            7,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRegistrationFails_DoesNotStoreRefreshToken()
    {
        // Arrange
        var request = AuthFaker.ValidRegisterRequest();
        _authService.RegisterAsync(default!, default!, default!)
            .ReturnsForAnyArgs(AuthErrors.RegistrationFailed("erro"));

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        await _authService.DidNotReceive().StoreRefreshTokenAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}