// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.CommandTests;

[TestFixture]
public class DeleteApplicationCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldDeleteApplication()
    {
        var application = new Application { ApplicationName = "test application", OperationalContextUri = OperationalContext.DefaultOperationalContextUri };
        Save(application);
        var applicationId = application.ApplicationId;

        Transaction(usersContext =>
        {
            var deleteApplicationCommand = new DeleteApplicationCommand(usersContext);
            deleteApplicationCommand.Execute(applicationId);
        });

        Transaction(usersContext => usersContext.Applications.Where(a => a.ApplicationId == applicationId).ToArray()).ShouldBeEmpty();
    }

    [Test]
    public void ShouldDeleteApplicationWithClient()
    {
        var application = new Application { ApplicationName = "test application", OperationalContextUri = OperationalContext.DefaultOperationalContextUri };

        var client = new ApiClient
        {
            Name = "test client",
            Key = "n/a",
            Secret = "n/a",
            ActivationCode = "fake activation code"
        };

        var clientAccessToken = new ClientAccessToken
        {
            ApiClient = client,
            Expiration = DateTime.Now.AddDays(1)
        };

        client.ClientAccessTokens.Add(clientAccessToken);

        application.ApiClients.Add(client);
        Save(application);

        var applicationId = application.ApplicationId;
        applicationId.ShouldBeGreaterThan(0);

        var clientId = client.ApiClientId;
        clientId.ShouldBeGreaterThan(0);

        var tokenId = clientAccessToken.Id;
        tokenId.ShouldNotBe(Guid.Empty);

        Transaction(usersContext =>
        {
            var deleteApplicationCommand = new DeleteApplicationCommand(usersContext);
            deleteApplicationCommand.Execute(applicationId);
        });

        Transaction(usersContext => usersContext.Applications.Where(a => a.ApplicationId == applicationId).ToArray()).ShouldBeEmpty();
        Transaction(usersContext => usersContext.Clients.Where(c => c.ApiClientId == clientId).ToArray()).ShouldBeEmpty();
    }

    [Test]
    public void ShouldDeleteApplicationWithOrganization()
    {
        var application = new Application { ApplicationName = "test application", OperationalContextUri = OperationalContext.DefaultOperationalContextUri };

        var client = new ApiClient
        {
            Name = "test client",
            Key = "n/a",
            Secret = "n/a",
        };

        var organization = new ApplicationEducationOrganization
        {
            Application = application,
            Clients = [client]
        };

        application.ApiClients.Add(client);
        application.ApplicationEducationOrganizations.Add(organization);
        Save(application);

        var applicationId = application.ApplicationId;
        applicationId.ShouldBeGreaterThan(0);

        var organizationId = organization.ApplicationEducationOrganizationId;
        organizationId.ShouldBeGreaterThan(0);

        Transaction(usersContext =>
        {
            var deleteApplicationCommand = new DeleteApplicationCommand(usersContext);
            deleteApplicationCommand.Execute(applicationId);
        });

        Transaction(usersContext => usersContext.Applications.Where(a => a.ApplicationId == applicationId).ToArray()).ShouldBeEmpty();
        Transaction(usersContext => usersContext.ApplicationEducationOrganizations.Where(o => o.ApplicationEducationOrganizationId == organizationId).ToArray()).ShouldBeEmpty();
    }

    [Test]
    public void ShouldDeleteApplicationWithProfile()
    {
        var application = new Application { ApplicationName = "test application", OperationalContextUri = OperationalContext.DefaultOperationalContextUri };
        var profile = new Profile { ProfileName = "test profile" };
        application.Profiles.Add(profile);

        Save(application);

        var applicationId = application.ApplicationId;
        applicationId.ShouldBeGreaterThan(0);

        var profileId = profile.ProfileId;
        profileId.ShouldBeGreaterThan(0);

        Transaction(usersContext =>
        {
            var deleteApplicationCommand = new DeleteApplicationCommand(usersContext);
            deleteApplicationCommand.Execute(applicationId);
        });

        Transaction(usersContext => usersContext.Applications.Where(a => a.ApplicationId == applicationId).ToArray()).ShouldBeEmpty();
        Transaction(usersContext => usersContext.Profiles.Where(p => p.ProfileId == profileId).ToArray()).ShouldNotBeEmpty();
    }
}
