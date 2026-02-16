using System;

static class CliParsing
{
    public static bool TryParseNonNegativeInt(string raw, string optionName, out int value)
    {
        if (!int.TryParse(raw, out value) || value < 0)
        {
            Console.Error.WriteLine($"Invalid value for {optionName}: '{raw}'. Expected a non-negative integer.");
            value = 0;
            return false;
        }

        return true;
    }

    public static bool TryParseUnitDouble(string raw, string optionName, out double value)
    {
        if (!double.TryParse(raw, out value) || value < 0 || value > 1)
        {
            Console.Error.WriteLine($"Invalid value for {optionName}: '{raw}'. Expected a number between 0 and 1.");
            value = 0;
            return false;
        }

        return true;
    }
}
