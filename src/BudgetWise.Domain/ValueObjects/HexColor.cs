using System.Text.RegularExpressions;
using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.ValueObjects;

public partial class HexColor : ValueObject
{
    private static readonly Regex HexPattern = HexRegex();

    public string Value { get; }

    private HexColor(string value) => Value = value;

    public static HexColor? Create(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null;

        var normalized = color.Trim().ToUpperInvariant();

        if (!HexPattern.IsMatch(normalized))
            throw new ArgumentException($"'{color}' não é uma cor hexadecimal válida. Use o formato #RRGGBB.");

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
