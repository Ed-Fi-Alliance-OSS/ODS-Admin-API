// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;

public static partial class StringExtensions
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

        return listOfStrings.Count != 0
            ? string.Join(separator, listOfStrings)
            : string.Empty;
    }

    public static object ToJsonObjectResponseDeleted(this string input)
    {
        return new { title = $"{input} deleted successfully" };
    }

    public static string ToPascalCase(this string input)
    {
        var matches = ToPascalCaseRegex().Match(input);
        var groupWords = matches.Groups["word"];

        var thread = Thread.CurrentThread.CurrentCulture.TextInfo;
        var stringBuilder = new StringBuilder();
        foreach (var captureWord in groupWords.Captures.Cast<Capture>())
            stringBuilder.Append(thread.ToTitleCase(captureWord.Value.ToLower()));
        return stringBuilder.ToString();
    }

    [GeneratedRegex("^(?<word>^[a-z]+|[A-Z]+|[A-Z][a-z]+)+$")]
    private static partial Regex ToPascalCaseRegex();
}
