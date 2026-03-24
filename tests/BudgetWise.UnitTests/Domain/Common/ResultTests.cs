using BudgetWise.Domain.Common.Results;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldBeSuccessful()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldNotBeSuccessful()
    {
        var error = Error.NotFound("Entity.NotFound", "Entity not found.");
        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void GenericSuccess_ShouldContainValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_ShouldNotContainValue()
    {
        var error = Error.Validation("Field.Required", "Field is required.");
        var result = Result.Failure<int>(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void GenericFailure_AccessingValue_ShouldThrowInvalidOperationException()
    {
        var result = Result.Failure<string>(Error.Validation("X", "Y"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Match_OnSuccess_ShouldCallOnSuccessFunc()
    {
        var result = Result<int>.Success(10);

        var output = result.Match(
            onSuccess: v => $"value={v}",
            onFailure: _ => "failed");

        output.Should().Be("value=10");
    }

    [Fact]
    public void Match_OnFailure_ShouldCallOnFailureFunc()
    {
        var error = Error.NotFound("X.NotFound", "Not found.");
        var result = Result<int>.Failure(error);

        var output = result.Match(
            onSuccess: _ => "success",
            onFailure: e => $"error={e.Code}");

        output.Should().Be("error=X.NotFound");
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldBeSuccess()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void ImplicitConversion_FromNullValue_ShouldBeFailureWithNullValueError()
    {
        Result<string> result = (string?)null;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldBeFailure()
    {
        var error = Error.Conflict("Name.Duplicate", "Name already exists.");
        Result<string> result = error;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
