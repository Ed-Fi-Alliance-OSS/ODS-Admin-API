using System.Globalization;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Extensions;

public static class StringExtensions
{
    public static string ToTitleCase(this string input)
        => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);

    public static string ToSingleEntity(this string input)
    {
        return input.Remove(input.Length - 1, 1);
    }

    public static string ToDelimiterSeparated(this IEnumerable<string> inputStrings, string separator = ",")
    {
        var listOfStrings = inputStrings.ToList();

        return listOfStrings.Any()
            ? string.Join(separator, listOfStrings)
            : string.Empty;
    }

    public static object ToJsonObjectResponseDeleted(this string input)
    {
        return new { title = $"{input} deleted successfully" };
    }
}
