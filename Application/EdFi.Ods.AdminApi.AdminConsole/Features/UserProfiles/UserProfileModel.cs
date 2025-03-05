// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.UserProfiles;
public class Extension
{
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("data")]
    public string Data { get; set; } = string.Empty;
}

public class Preference
{
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;
}

public class UserProfileModel
{
    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonProperty("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("preferences")]
    public List<Preference>? Preferences { get; set; }

    [JsonProperty("extensions")]
    public List<Extension>? Extensions { get; set; }

    [JsonProperty("tenants")]
    public List<UserProfileTenant>? Tenants { get; set; }

    [JsonProperty("selectedTenant")]
    public SelectedTenant? SelectedTenant { get; set; }

    [JsonProperty("tenantsTotalCount")]
    public int? TenantsTotalCount { get; set; }
}

public class SelectedTenant
{
    [JsonProperty("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonProperty("createdDateTime")]
    public DateTime? CreatedDateTime { get; set; }

    [JsonProperty("domains")]
    public List<string>? Domains { get; set; }

    [JsonProperty("isDemo")]
    public bool? IsDemo { get; set; }

    [JsonProperty("isIdentityProviders")]
    public List<string>? IsIdentityProviders { get; set; }

    [JsonProperty("lastModifiedBy")]
    public string LastModifiedBy { get; set; } = string.Empty;

    [JsonProperty("lastModifiedDateTime")]
    public DateTime? LastModifiedDateTime { get; set; }

    [JsonProperty("organizationIdentifier")]
    public string OrganizationIdentifier { get; set; } = string.Empty;

    [JsonProperty("organizationName")]
    public string OrganizationName { get; set; } = string.Empty;

    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;

    [JsonProperty("subscriptions")]
    public List<object>? Subscriptions { get; set; }

    [JsonProperty("subscriptionsMigrated")]
    public bool? SubscriptionsMigrated { get; set; }

    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonProperty("tenantStatus")]
    public string TenantStatus { get; set; } = string.Empty;

    [JsonProperty("tenantType")]
    public string TenantType { get; set; } = string.Empty;
}

public class UserProfileTenant
{
    [JsonProperty("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonProperty("createdDateTime")]
    public DateTime? CreatedDateTime { get; set; }

    [JsonProperty("domains")]
    public List<string>? Domains { get; set; }

    [JsonProperty("isDemo")]
    public bool? IsDemo { get; set; }

    [JsonProperty("isIdentityProviders")]
    public List<string>? IsIdentityProviders { get; set; }

    [JsonProperty("lastModifiedBy")]
    public string LastModifiedBy { get; set; } = string.Empty;

    [JsonProperty("lastModifiedDateTime")]
    public DateTime? LastModifiedDateTime { get; set; }

    [JsonProperty("organizationIdentifier")]
    public string OrganizationIdentifier { get; set; } = string.Empty;

    [JsonProperty("organizationName")]
    public string OrganizationName { get; set; } = string.Empty;

    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;

    [JsonProperty("subscriptions")]
    public List<object>? Subscriptions { get; set; }

    [JsonProperty("subscriptionsMigrated")]
    public bool? SubscriptionsMigrated { get; set; }

    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonProperty("tenantStatus")]
    public string TenantStatus { get; set; } = string.Empty;

    [JsonProperty("tenantType")]
    public string TenantType { get; set; } = string.Empty;
}

