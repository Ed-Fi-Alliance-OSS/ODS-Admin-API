// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Documentation;

public class SwaggerDefaultParameterFilter : IOperationFilter
{
    private readonly IOptions<AppSettings> _settings;

    public SwaggerDefaultParameterFilter(IOptions<AppSettings> settings)
    {
        _settings = settings;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameter in operation.Parameters ?? [])
        {
            var schema = parameter.Schema as OpenApiSchema;

            if (parameter.Name?.ToLower() == "offset")
            {
                parameter.Description = "Indicates how many items should be skipped before returning results.";
                if (schema is not null)
                    schema.Default = JsonValue.Create(_settings.Value.DefaultPageSizeOffset.ToString());
            }
            else if (parameter.Name?.ToLower() == "limit")
            {
                parameter.Description = "Indicates the maximum number of items that should be returned in the results.";
                if (schema is not null)
                    schema.Default = JsonValue.Create(_settings.Value.DefaultPageSizeLimit.ToString());
            }
            else if (parameter.Name?.ToLower() == "orderby")
            {
                parameter.Description = "Indicates the property name by which the results will be sorted.";
                if (schema is not null)
                    schema.Default = JsonValue.Create(string.Empty);
            }
            else if (parameter.Name?.ToLower() == "direction")
            {
                var description = "Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).";
                parameter.Description = description;
                if (schema is not null)
                {
                    schema.Title = description;
                    schema.Enum = new List<JsonNode> { JsonValue.Create(SortingDirectionHelper.Direction.Ascending.ToString()), JsonValue.Create(SortingDirectionHelper.Direction.Descending.ToString()) };
                    schema.Default = JsonValue.Create(SortingDirectionHelper.Direction.Descending.ToString());
                }
            }
        }

        switch (context.MethodInfo.Name)
        {
            case "GetProfiles":
                {
                    foreach (var parameter in operation.Parameters ?? [])
                    {
                        if (parameter.Name?.ToLower() == "id")
                        {
                            parameter.Description = FeatureConstants.ProfileIdDescription;
                        }
                        else if (parameter.Name?.ToLower() == "name")
                        {
                            parameter.Description = FeatureConstants.ProfileName;
                        }
                    }
                    break;
                }
            case "GetResourceClaims":
                {
                    foreach (var parameter in operation.Parameters ?? [])
                    {
                        if (parameter.Name?.ToLower() == "id")
                        {
                            parameter.Description = FeatureConstants.ResourceClaimIdDescription;
                        }
                        else if (parameter.Name?.ToLower() == "name")
                        {
                            parameter.Description = FeatureConstants.ResourceClaimNameDescription;
                        }
                    }
                    break;
                }
            case "GetVendors":
                {
                    foreach (var parameter in operation.Parameters ?? [])
                    {
                        if (parameter.Name?.ToLower() == "id")
                        {
                            parameter.Description = FeatureConstants.VendorIdDescription;
                        }
                        else if (parameter.Name?.ToLower() == "company")
                        {
                            parameter.Description = FeatureConstants.VendorNameDescription;
                        }
                        else if (parameter.Name?.ToLower() == "namespaceprefixes")
                        {
                            parameter.Description = FeatureConstants.VendorNamespaceDescription;
                        }
                        else if (parameter.Name?.ToLower() == "contactname")
                        {
                            parameter.Description = FeatureConstants.VendorContactDescription;
                        }
                        else if (parameter.Name?.ToLower() == "contactemailaddress")
                        {
                            parameter.Description = FeatureConstants.VendorContactEmailDescription;
                        }
                    }
                    break;
                }
            case "GetDataStores":
                {
                    foreach (var parameter in operation.Parameters ?? [])
                    {
                        if (parameter.Name?.ToLower() == "id")
                        {
                            parameter.Description = FeatureConstants.DataStoreIdsDescription;
                        }
                        else if (parameter.Name?.ToLower() == "name")
                        {
                            parameter.Description = FeatureConstants.DataStoreName;
                        }
                    }
                    break;
                }
            case "GetClaimSets":
                {
                    foreach (var parameter in operation.Parameters ?? [])
                    {
                        if (parameter.Name?.ToLower() == "id")
                        {
                            parameter.Description = FeatureConstants.ClaimSetIdDescription;
                        }
                        else if (parameter.Name?.ToLower() == "name")
                        {
                            parameter.Description = FeatureConstants.ClaimSetNameDescription;
                        }
                    }
                    break;
                }
            case "GetApplications":
                {
                    foreach (var parameter in operation.Parameters ?? [])
                    {
                        if (parameter.Name?.ToLower() == "id")
                        {
                            parameter.Description = FeatureConstants.ApplicationIdDescription;
                        }
                        else if (parameter.Name?.ToLower() == "applicationname")
                        {
                            parameter.Description = FeatureConstants.ApplicationNameDescription;
                        }
                        else if (parameter.Name?.ToLower() == "claimsetname")
                        {
                            parameter.Description = FeatureConstants.ClaimSetNameDescription;
                        }
                    }
                    break;
                }
            case "GetActions":
                {
                    foreach (var parameter in operation.Parameters ?? [])
                    {
                        if (parameter.Name?.ToLower() == "id")
                        {
                            parameter.Description = FeatureConstants.ActionIdDescription;
                        }
                        else if (parameter.Name?.ToLower() == "name")
                        {
                            parameter.Description = FeatureConstants.ActionNameDescription;
                        }
                    }
                    break;
                }
        }
    }
}



