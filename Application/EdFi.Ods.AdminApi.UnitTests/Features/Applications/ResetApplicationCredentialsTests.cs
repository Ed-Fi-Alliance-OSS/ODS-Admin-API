// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Applications;
using FluentValidation;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Applications;

[TestFixture]
public class ResetApplicationCredentialsTests
{
    [Test]
    public void HandleResetCredentials_WhenEndpointDisabled_ThrowsValidationException()
    {
        var settings = Options.Create(new AppSettings { EnableApplicationResetEndpoint = false });

        var exception = Should.Throw<ValidationException>(async () =>
            await ResetApplicationCredentials.HandleResetCredentials(null!, settings, 1));

        exception.Errors.Single().PropertyName.ShouldBe(nameof(Application));
    }
}
