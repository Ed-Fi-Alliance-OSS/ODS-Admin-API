// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;

namespace EdFi.Ods.AdminApi.Common.Settings;

public class ReverseProxySettings
{
    public bool UseForwardedHeaders { get; set; }
    public string KnownProxies { get; set; } = string.Empty;
    public string KnownNetworks { get; set; } = string.Empty;

    public IEnumerable<IPAddress> GetKnownProxies() =>
        KnownProxies
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(IPAddress.Parse);

    public IEnumerable<IPNetwork> GetKnownNetworks() =>
        KnownNetworks
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(IPNetwork.Parse);
}
