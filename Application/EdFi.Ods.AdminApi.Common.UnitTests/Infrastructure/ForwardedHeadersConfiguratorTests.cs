// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure;

[TestFixture]
public class ForwardedHeadersConfiguratorTests
{
    [Test]
    public void Configure_SetsForwardedForHostAndProtoHeaders()
    {
        var options = new ForwardedHeadersOptions();

        ForwardedHeadersConfigurator.Configure(options, new ReverseProxySettings());

        options.ForwardedHeaders.ShouldBe(
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto);
    }

    [Test]
    public void Configure_WithEmptySettings_LeavesFrameworkDefaultKnownProxiesAndNetworksInPlace()
    {
        var options = new ForwardedHeadersOptions();
        var defaultProxyCount = options.KnownProxies.Count;
        var defaultNetworkCount = options.KnownIPNetworks.Count;

        ForwardedHeadersConfigurator.Configure(options, new ReverseProxySettings());

        options.KnownProxies.Count.ShouldBe(defaultProxyCount);
        options.KnownIPNetworks.Count.ShouldBe(defaultNetworkCount);
    }

    [Test]
    public void Configure_WithKnownProxies_AddsThemToOptions()
    {
        var options = new ForwardedHeadersOptions();
        var settings = new ReverseProxySettings { KnownProxies = "10.0.0.1, 10.0.0.2" };

        ForwardedHeadersConfigurator.Configure(options, settings);

        options.KnownProxies.ShouldContain(IPAddress.Parse("10.0.0.1"));
        options.KnownProxies.ShouldContain(IPAddress.Parse("10.0.0.2"));
    }

    [Test]
    public void Configure_WithKnownNetworks_AddsThemToOptions()
    {
        var options = new ForwardedHeadersOptions();
        var settings = new ReverseProxySettings { KnownNetworks = "172.16.0.0/12,192.168.0.0/16" };

        ForwardedHeadersConfigurator.Configure(options, settings);

        options.KnownIPNetworks.ShouldContain(
            new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
        options.KnownIPNetworks.ShouldContain(
            new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
    }
}
