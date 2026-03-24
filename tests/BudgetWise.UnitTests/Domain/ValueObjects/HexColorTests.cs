using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.ValueObjects;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.ValueObjects;

public sealed class HexColorTests
{
    [Theory]
    [InlineData("#FF5733")]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#AABBCC")]
    public void Create_WithValidHex_ShouldReturnSuccess(string value)
    {
        var result = HexColor.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value.ToUpperInvariant());
    }

    [Theory]
    [InlineData("#ff5733")]
    [InlineData("#aabbcc")]
    public void Create_WithLowercase_ShouldNormalizeToUppercase(string value)
    {
        var result = HexColor.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value.ToUpperInvariant());
    }

    [Fact]
    public void Create_WithLeadingAndTrailingSpaces_ShouldTrimAndSucceed()
    {
        var result = HexColor.Create("  #FF5733  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("#FF5733");
    }

    [Fact]
    public void Create_WithNull_ShouldReturnValidationError()
    {
        var result = HexColor.Create(null);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("HexColor.Invalid");
    }

    [Fact]
    public void Create_WithEmptyString_ShouldReturnValidationError()
    {
        var result = HexColor.Create(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_WithWhitespace_ShouldReturnValidationError()
    {
        var result = HexColor.Create("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData("FF5733")]       // sem #
    [InlineData("#FF573")]       // 5 dígitos
    [InlineData("#FF573300")]    // 8 dígitos
    [InlineData("#GGHHII")]      // caracteres inválidos
    [InlineData("red")]          // nome de cor
    [InlineData("#FFF")]         // hex curto (3 dígitos)
    public void Create_WithInvalidFormat_ShouldReturnValidationError(string value)
    {
        var result = HexColor.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void ImplicitOperator_ShouldReturnStringValue()
    {
        var hexColor = HexColor.Create("#FF5733").Value;

        string? value = hexColor;

        value.Should().Be("#FF5733");
    }

    [Fact]
    public void ImplicitOperator_WhenNull_ShouldReturnNull()
    {
        HexColor? hexColor = null;

        string? value = hexColor;

        value.Should().BeNull();
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var hexColor = HexColor.Create("#FF5733").Value;

        hexColor.ToString().Should().Be("#FF5733");
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var a = HexColor.Create("#FF5733").Value;
        var b = HexColor.Create("#FF5733").Value;

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        var a = HexColor.Create("#FF5733").Value;
        var b = HexColor.Create("#AABBCC").Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_LowercaseVsUppercase_ShouldBeEqual()
    {
        var a = HexColor.Create("#ff5733").Value;
        var b = HexColor.Create("#FF5733").Value;

        a.Should().Be(b);
    }
}
