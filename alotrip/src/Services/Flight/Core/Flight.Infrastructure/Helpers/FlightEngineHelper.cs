using System.Globalization;
using System.Security.Cryptography;

namespace Flight.Infrastructure.Helpers;

/// <summary>
/// General-purpose helpers ported from IBE.Common and Business.Utility.Utilities.
/// </summary>
public static class FlightEngineHelper
{
    private static readonly string[] MonthNames =
        ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"];

    /// <summary>
    /// Converts a DateTime to the GDS English format: ddMONyy (e.g. 05JAN25).
    /// </summary>
    public static string ConvertEnglishDateTime(DateTime input)
        => $"{input.Day:00}{MonthNames[input.Month - 1]}{input.Year.ToString()[2..]}";

    /// <summary>
    /// Parses a date+time string where day is "yyyyMMdd" and time is "HHmm".
    /// Dashes in day are replaced with slashes before parsing.
    /// </summary>
    public static DateTime GetDate(string day, string time)
    {
        day = day.Replace("-", "/");
        if (time.Length < 4)
            time = int.Parse(time).ToString("0000");

        const string format = "yyyyMMdd HHmm";
        if (DateTime.TryParseExact(
                day + " " + time, format,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        return DateTime.Now;
    }

    /// <summary>
    /// Parses a date+time string with a custom format.
    /// </summary>
    public static DateTime GetDate(string day, string time, string format)
    {
        if (time.Length < 4)
            time = int.Parse(time).ToString("0000");

        if (DateTime.TryParseExact(
                day + " " + time, format,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        return DateTime.Now;
    }

    /// <summary>
    /// Parses a date-only string with a custom format.
    /// </summary>
    public static DateTime GetDateFormat(string day, string format)
    {
        DateTime.TryParseExact(day, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt);
        return dt;
    }

    /// <summary>
    /// Strips Vietnamese diacritics from a string (used for name normalization).
    /// </summary>
    public static string ConvertToUnSignString(string input)
    {
        const string findText =
            "áàảãạâấầẩẫậăắằẳẵặđéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵ" +
            "ÁÀẢÃẠÂẤẦẨẪẬĂẮẰẲẴẶĐÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴ";
        const string replText =
            "aaaaaaaaaaaaaaaaadeeeeeeeeeeeiiiiiooooooooooooooooouuuuuuuuuuuyyyy" +
            "yAAAAAAAAAAAAAAAAADEEEEEEEEEEEIIIIIOOOOOOOOOOOOOOOOOUUUUUUUUUUUYYYYY";

        var chars = findText.ToCharArray();
        int idx;
        while ((idx = input.IndexOfAny(chars)) != -1)
        {
            int idx2 = findText.IndexOf(input[idx]);
            input = input.Replace(input[idx], replText[idx2]);
        }
        return input.Replace("'", "");
    }

    /// <summary>
    /// Generates a random alphanumeric string of the given length.
    /// Uses <see cref="RandomNumberGenerator"/> instead of the old seeded Random for thread safety.
    /// </summary>
    public static string GetRandomAlphanumeric(int length)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
        return RandomNumberGenerator.GetString(chars, length);
    }

    /// <summary>
    /// Rounds a fare amount according to currency:
    ///   VND  → nearest 1 000
    ///   other → ceiling to whole unit
    /// </summary>
    public static double RoundFare(double amount, string currencyCode)
        => currencyCode == "VND"
            ? Math.Round(amount / 1_000) * 1_000
            : Math.Ceiling(amount);

    /// <summary>
    /// Chunks a list into sublists of at most <paramref name="chunkSize"/> elements.
    /// </summary>
    public static List<List<T>> Chunk<T>(IList<T> source, int chunkSize)
        => source
            .Select((x, i) => new { x, g = i / chunkSize })
            .GroupBy(p => p.g, p => p.x)
            .Select(g => g.ToList())
            .ToList();
}
