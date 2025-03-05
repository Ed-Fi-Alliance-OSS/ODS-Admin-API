// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

//using System.Dynamic;
//using Newtonsoft.Json;

//namespace EdFi.Ods.AdminApi.AdminConsole.Helpers;

//public class ExpandoObjectConverter : JsonConverter
//{
//    public override bool CanConvert(Type objectType)
//    {
//        return typeof(ExpandoObject).IsAssignableFrom(objectType);
//    }

//    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//    {
//        var dictionary = (IDictionary<string, object>)value;
//        writer.WriteStartObject();

//        foreach (var kvp in dictionary)
//        {
//            writer.WritePropertyName(kvp.Key);
//            serializer.Serialize(writer, kvp.Value);
//        }

//        writer.WriteEndObject();
//    }

//    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//    {
//        return serializer.Deserialize<ExpandoObject>(reader);
//    }
//}
