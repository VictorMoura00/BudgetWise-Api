using BudgetWise.Domain.Common.Results;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Common;

public sealed class ErrorTests
{
    [Fact]
    public void None_ShouldHaveEmptyCodeAndDescription_AndTypeNone()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Description.Should().BeEmpty();
        Error.None.Type.Should().Be(ErrorType.None);
    }

    [Fact]
    public void NullValue_ShouldHaveValidationTypeAndNonEmptyCode()
    {
        Error.NullValue.Code.Should().Be("Error.NullValue");
        Error.NullValue.Type.Should().Be(ErrorType.Validation);
        Error.NullValue.Description.Should().NotBeEmpty();
    }

    [Fact]
    public void Failure_ShouldSetCodeDescriptionAndType()
    {
        var error = Error.Failure("Op.Failed", "Operation failed.");

        error.Code.Should().Be("Op.Failed");
        error.Description.Should().Be("Operation failed.");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Validation_ShouldSetValidationType()
    {
        var error = Error.Validation("Field.Invalid", "Field is invalid.");

        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("Field.Invalid");
    }

    [Fact]
    public void NotFound_ShouldSetNotFoundType()
    {
        var error = Error.NotFound("User.NotFound", "User not found.");

        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public void Conflict_ShouldSetConflictType()
    {
        var error = Error.Conflict("Email.Duplicate", "Email already in use.");

        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("Email.Duplicate");
    }

    [Fact]
    public void Unauthorized_ShouldSetUnauthorizedType()
    {
        var error = Error.Unauthorized("Token.Invalid", "Token is invalid.");

        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Code.Should().Be("Token.Invalid");
    }

    [Fact]
    public void TwoErrors_WithSameCodeAndDescription_ShouldBeEqual()
    {
        var a = Error.NotFound("X.NotFound", "Not found.");
        var b = Error.NotFound("X.NotFound", "Not found.");

        a.Should().Be(b);
    }

    [Fact]
    public void TwoErrors_WithDifferentCode_ShouldNotBeEqual()
    {
        var a = Error.NotFound("A.NotFound", "Not found.");
        var b = Error.NotFound("B.NotFound", "Not found.");

        a.Should().NotBe(b);
    }
}
