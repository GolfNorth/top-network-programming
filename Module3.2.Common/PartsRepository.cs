namespace Module3_2.Common;

public static class PartsRepository
{
    public static readonly IReadOnlyDictionary<string, decimal> Parts =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["CPU"]         = 45_000,
            ["GPU"]         = 130_000,
            ["RAM"]         = 12_000,
            ["SSD"]         = 7_000,
            ["HDD"]         = 4_500,
            ["Motherboard"] = 25_000,
            ["PSU"]         = 8_000,
            ["Case"]        = 9_000,
            ["Cooler"]      = 6_000,
            ["Monitor"]     = 35_000,
        };

    public static decimal? GetPrice(string part) =>
        Parts.TryGetValue(part, out var price) ? price : null;
}
