// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Features.ClaimSets;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.JsonContractResolvers;

public class ShouldSerializeContractResolver : DefaultContractResolver
{
    public ShouldSerializeContractResolver() : base()
    {
    }
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        if (property.DeclaringType == typeof(ResourceClaimModel) && 
            string.Equals(property.PropertyName, "readChanges", StringComparison.OrdinalIgnoreCase))
        {
            property.ShouldSerialize = _ => true;
        }

        property.PropertyName = char.ToLowerInvariant(property.PropertyName![0]) + property.PropertyName[1..];
        return property;
    }
}
