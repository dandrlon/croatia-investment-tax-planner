namespace TaxPlanner.Core.Rules;

internal static class Money
{
    /// <summary>Rounds a final monetary figure to cents. Apply once, to sums, never per row.</summary>
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal? Round(decimal? value) => value is null ? null : Round(value.Value);
}
