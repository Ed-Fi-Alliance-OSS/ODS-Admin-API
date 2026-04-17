// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
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
        foreach (var parameter in operation.Parameters)
        {
            if (parameter.Name.ToLower().Equals("offset"))
            {
                parameter.Description = "Indicates how many items should be skipped before returning results.";
                parameter.Schema.Default = new OpenApiString(_settings.Value.DefaultPageSizeOffset.ToString());
            }
            else if (parameter.Name.ToLower().Equals("limit"))
            {
                parameter.Description = "Indicates the maximum number of items that should be returned in the results.";
                parameter.Schema.Default = new OpenApiString(_settings.Value.DefaultPageSizeLimit.ToString());
            }
            else if (parameter.Name.ToLower().Equals("orderby"))
            {
                parameter.Description = "Indicates the property name by which the results will be sorted.";
                parameter.Schema.Default = new OpenApiString(string.Empty);
            }
            else if (parameter.Name.ToLower().Equals("direction"))
            {
                var description = "Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).";
                parameter.Schema.Title = description;
                parameter.Description = description;
                parameter.Schema.Enum = new List<IOpenApiAny> { new OpenApiString(SortingDirectionHelper.Direction.Ascending.ToString()), new OpenApiString(SortingDirectionHelper.Direction.Descending.ToString()) };
                parameter.Schema.Default = new OpenApiString(SortingDirectionHelper.Direction.Descending.ToString());
            }
        }

        switch (context.MethodInfo.Name)
        {
            case "GetProfiles":
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Name.ToLower().Equals("id"))
                        {
                            parameter.Description = FeatureConstants.ProfileIdDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("name"))
                        {
                            parameter.Description = FeatureConstants.ProfileName;
                        }
                    }
                    break;
                }
            case "GetResourceClaims":
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Name.ToLower().Equals("id"))
                        {
                            parameter.Description = FeatureConstants.ResourceClaimIdDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("name"))
                        {
                            parameter.Description = FeatureConstants.ResourceClaimNameDescription;
                        }
                    }
                    break;
                }
            case "GetVendors":
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Name.ToLower().Equals("id"))
                        {
                            parameter.Description = FeatureConstants.VendorIdDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("company"))
                        {
                            parameter.Description = FeatureConstants.VendorNameDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("namespaceprefixes"))
                        {
                            parameter.Description = FeatureConstants.VendorNamespaceDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("contactname"))
                        {
                            parameter.Description = FeatureConstants.VendorContactDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("contactemailaddress"))
                        {
                            parameter.Description = FeatureConstants.VendorContactEmailDescription;
                        }
                    }
                    break;
                }
            case "GetOdsInstances":
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Name.ToLower().Equals("id"))
                        {
                            parameter.Description = FeatureConstants.OdsInstanceIdsDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("name"))
                        {
                            parameter.Description = FeatureConstants.OdsInstanceName;
                        }
                    }
                    break;
                }
            case "GetClaimSets":
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Name.ToLower().Equals("id"))
                        {
                            parameter.Description = FeatureConstants.ClaimSetIdDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("name"))
                        {
                            parameter.Description = FeatureConstants.ClaimSetNameDescription;
                        }
                    }
                    break;
                }
            case "GetApplications":
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Name.ToLower().Equals("id"))
                        {
                            parameter.Description = FeatureConstants.ApplicationIdDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("applicationname"))
                        {
                            parameter.Description = FeatureConstants.ApplicationNameDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("claimsetname"))
                        {
                            parameter.Description = FeatureConstants.ClaimSetNameDescription;
                        }
                    }
                    break;
                }
            case "GetActions":
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Name.ToLower().Equals("id"))
                        {
                            parameter.Description = FeatureConstants.ActionIdDescription;
                        }
                        else if (parameter.Name.ToLower().Equals("name"))
                        {
                            parameter.Description = FeatureConstants.ActionNameDescription;
                        }
                    }
                    break;
                }
        }
    }
}
