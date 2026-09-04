// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace EdFi.Ods.AdminApi.Common.Infrastructure;

public static class ForwardedHeadersConfigurator
{
    public static void Configure(ForwardedHeadersOptions options, ReverseProxySettings settings)
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;

        // Framework defaults (loopback) are intentionally left in place, so an
        // enabled-but-unconfigured deployment trusts loopback only, not every source.
        foreach (var proxy in settings.GetKnownProxies())
        {
            options.KnownProxies.Add(proxy);
        }

        foreach (var network in settings.GetKnownNetworks())
        {
            options.KnownIPNetworks.Add(network);
        }
    }
}
