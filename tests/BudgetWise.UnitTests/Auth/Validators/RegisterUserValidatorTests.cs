using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Auth.Validators;
using FluentAssertions;

namespace BudgetWise.UnitTests.Auth.Validators;

public sealed class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _sut = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldBeValid()
    {
        // Arrange
        var request = new RegisterUserRequest("João Silva", "joao@email.com", "Senha@123", "Senha@123");

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyFullName_ShouldBeInvalid(string fullName)
    {
        // Arrange
        var request = new RegisterUserRequest(fullName, "joao@email.com", "Senha@123", "Senha@123");

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.FullName));
    }

    [Fact]
    public void Validate_WithFullNameExceeding150Chars_ShouldBeInvalid()
    {
        // Arrange
        var request = new RegisterUserRequest(new string('a', 151), "joao@email.com", "Senha@123", "Senha@123");

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.FullName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("email-invalido")]
    [InlineData("sem-arroba.com")]
    public void Validate_WithInvalidEmail_ShouldBeInvalid(string email)
    {
        // Arrange
        var request = new RegisterUserRequest("João Silva", email, "Senha@123", "Senha@123");

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Email));
    }

    [Theory]
    [InlineData("curta1@", "Menos de 8 caracteres")]
    [InlineData("semmaius@1", "Sem maiúscula")]
    [InlineData("SEMMINUSCULA@1", "Sem minúscula")]
    [InlineData("SemNumero@", "Sem número")]
    [InlineData("SemEspecial1", "Sem caractere especial")]
    public void Validate_WithWeakPassword_ShouldBeInvalid(string password, string _)
    {
        // Arrange
        var request = new RegisterUserRequest("João Silva", "joao@email.com", password, password);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Password));
    }

    [Fact]
    public void Validate_WhenConfirmPasswordDiffers_ShouldBeInvalid()
    {
        // Arrange
        var request = new RegisterUserRequest("João Silva", "joao@email.com", "Senha@123", "Diferente@123");

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ConfirmPassword));
    }
}
