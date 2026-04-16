using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Auth.Validators;
using FluentAssertions;

namespace BudgetWise.UnitTests.Auth.Validators;

public sealed class RefreshTokenValidatorTests
{
    private readonly RefreshTokenValidator _sut = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldBeValid()
    {
        // Arrange
        var request = new RefreshTokenRequest(Guid.NewGuid(), "token-valido-qualquer");

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldBeInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest(Guid.Empty, "token-valido-qualquer");

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.UserId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyRefreshToken_ShouldBeInvalid(string refreshToken)
    {
        // Arrange
        var request = new RefreshTokenRequest(Guid.NewGuid(), refreshToken);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.RefreshToken));
    }
}
