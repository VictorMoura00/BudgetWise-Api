using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Common.Results;
using System.Text.RegularExpressions;

namespace BudgetWise.Domain.ValueObjects;

public partial class HexColor : ValueObject
{
    private static readonly Regex HexPattern = HexRegex();

    public string Value { get; }

    private HexColor(string value) => Value = value;

    public static Result<HexColor> Create(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return Error.Validation("HexColor.Invalid", "Color cannot be empty.");

        var normalized = color.Trim().ToUpperInvariant();

        if (!HexPattern.IsMatch(normalized))
            return Error.Validation("HexColor.Invalid", $"'{color}' is not a valid hex color. Use the format #RRGGBB.");

        return new HexColor(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string?(HexColor? color) => color?.Value;

    [GeneratedRegex(@"^#[0-9A-F]{6}$")]
    private static partial Regex HexRegex();
}
