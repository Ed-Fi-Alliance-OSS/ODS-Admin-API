// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Settings;

public class AppSettingsFile
{
    public required AppSettings AppSettings { get; set; }
    public required SwaggerSettings SwaggerSettings { get; set; }
    public Dictionary<string, string> ConnectionStrings { get; set; } = [];
    public Dictionary<string, TenantSettings> Tenants { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public required TestingSettings Testing { get; set; }
}

public class AppSettings
{
    public int DefaultPageSizeOffset { get; set; }
    public int DefaultPageSizeLimit { get; set; }
    public string? DatabaseEngine { get; set; }
    public string? EncryptionKey { get; set; }
    public bool MultiTenancy { get; set; }
    public bool PreventDuplicateApplications { get; set; }
    public bool EnableApplicationResetEndpoint { get; set; }
    public int EdOrgsRefreshIntervalInMins { get; set; }
    public string? AdminApiMode { get; set; }
}

public class SwaggerSettings
{
    public bool EnableSwagger { get; set; }
    public string? DefaultTenant { get; set; }
}


public class IpRateLimitingOptions
{
    public bool EnableEndpointRateLimiting { get; set; }
    public bool StackBlockedRequests { get; set; }
    public string RealIpHeader { get; set; } = string.Empty;
    public string ClientIdHeader { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; }
    public List<string>? IpWhitelist { get; set; }
    public List<string>? EndpointWhitelist { get; set; }
    public List<GeneralRule>? GeneralRules { get; set; }

    public class GeneralRule
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public int Limit { get; set; }
    }
}
