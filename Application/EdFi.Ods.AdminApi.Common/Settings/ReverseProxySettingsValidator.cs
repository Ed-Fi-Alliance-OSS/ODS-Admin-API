// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Settings;

public class ReverseProxySettingsValidator : IValidateOptions<ReverseProxySettings>
{
    public ValidateOptionsResult Validate(string? name, ReverseProxySettings options)
    {
        try
        {
            _ = options.GetKnownProxies().ToList();
            _ = options.GetKnownNetworks().ToList();
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail(
                $"ReverseProxy contains an invalid KnownProxies/KnownNetworks entry: {ex.Message}");
        }

        return ValidateOptionsResult.Success;
    }
}
