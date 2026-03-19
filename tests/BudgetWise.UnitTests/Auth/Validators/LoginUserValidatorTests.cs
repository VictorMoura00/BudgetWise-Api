using BudgetWise.Application.Auth.Login;
using FluentAssertions;

namespace BudgetWise.UnitTests.Auth.Validators;

public sealed class LoginUserValidatorTests
{
    private readonly LoginUserValidator _sut = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldBeValid()
    {
        // Arrange
        var request = new LoginUserRequest("joao@email.com", "Senha@123");

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("email-invalido")]
    [InlineData("sem-arroba.com")]
    public void Validate_WithInvalidEmail_ShouldBeInvalid(string email)
    {
        // Arrange
        var request = new LoginUserRequest(email, "Senha@123");

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPassword_ShouldBeInvalid(string password)
    {
        // Arrange
        var request = new LoginUserRequest("joao@email.com", password);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Password));
    }
}
