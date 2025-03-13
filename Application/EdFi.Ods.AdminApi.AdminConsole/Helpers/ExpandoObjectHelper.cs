// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Dynamic;
using System.Text.Json;
using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.AdminConsole.Helpers
{
    public static class ExpandoObjectHelper
    {
        public static ExpandoObject NormalizeExpandoObject(ExpandoObject expando, Exception exception)
        {
            var normalized = new ExpandoObject() as IDictionary<string, object>;

            if (expando is IDictionary<string, object> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    if (kvp.Value is JsonElement jsonElement)
                    {
                        object? jsonElementValue = jsonElement.ValueKind switch
                        {
                            JsonValueKind.String => jsonElement.GetString(),
                            JsonValueKind.Number => jsonElement.GetDecimal(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            JsonValueKind.Object => JsonConvert.DeserializeObject<ExpandoObject>(jsonElement.GetRawText()),
                            _ => jsonElement.ToString()
                        };

                        normalized[kvp.Key] = jsonElementValue ?? jsonElement.ToString();
                    }
                    else if (kvp.Value is ExpandoObject nestedExpando)
                    {
                        normalized[kvp.Key] = NormalizeExpandoObject(nestedExpando, new Exception());
                    }
                    else
                    {
                        normalized[kvp.Key] = kvp.Value;
                    }
                }
            }

            return normalized as ExpandoObject ?? throw exception;
        }

    }
}
