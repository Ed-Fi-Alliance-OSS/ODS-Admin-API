// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Settings;

[TestFixture]
public class ReverseProxySettingsValidatorTests
{
    private readonly ReverseProxySettingsValidator _validator = new();

    [Test]
    public void Validate_WithEmptySettings_Succeeds()
    {
        var result = _validator.Validate(null, new ReverseProxySettings());

        result.Succeeded.ShouldBeTrue();
    }

    [Test]
    public void Validate_WithValidProxiesAndNetworks_Succeeds()
    {
        var settings = new ReverseProxySettings
        {
            KnownProxies = "10.0.0.1, 10.0.0.2",
            KnownNetworks = "172.16.0.0/12,192.168.0.0/16"
        };

        var result = _validator.Validate(null, settings);

        result.Succeeded.ShouldBeTrue();
    }

    [Test]
    public void Validate_WithMalformedKnownProxy_Fails()
    {
        var settings = new ReverseProxySettings { KnownProxies = "not-an-ip" };

        var result = _validator.Validate(null, settings);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("KnownProxies");
    }

    [Test]
    public void Validate_WithMalformedKnownNetwork_Fails()
    {
        var settings = new ReverseProxySettings { KnownNetworks = "10.0.0.0/not-a-prefix" };

        var result = _validator.Validate(null, settings);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("KnownProxies");
    }

    [Test]
    public void Validate_WithOutOfRangePrefixLength_Fails()
    {
        var settings = new ReverseProxySettings { KnownNetworks = "10.0.0.0/33" };

        var result = _validator.Validate(null, settings);

        result.Failed.ShouldBeTrue();
    }

    [Test]
    public void Validate_WithTrailingSegmentAfterPrefix_Fails()
    {
        var settings = new ReverseProxySettings { KnownNetworks = "10.0.0.0/24/typo" };

        var result = _validator.Validate(null, settings);

        result.Failed.ShouldBeTrue();
    }
}
