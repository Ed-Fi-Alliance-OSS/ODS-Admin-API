// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetEducationOrganizationsQueryTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"GetEducationOrganizationsQueryTests_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "Postgres"
            })
            .Build();

        return new AdminApiDbContext(options, configuration);
    }

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public async Task ExecuteAsync_WithoutFilters_GroupsEducationOrganizationsByInstance()
    {
        using var context = CreateContext();
        context.EducationOrganizations.AddRange(
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Sandbox A",
                EducationOrganizationId = 1001,
                NameOfInstitution = "North High",
                Discriminator = "School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Sandbox A",
                EducationOrganizationId = 1002,
                NameOfInstitution = "North Middle",
                Discriminator = "School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 2,
                InstanceName = "Sandbox B",
                EducationOrganizationId = 2001,
                NameOfInstitution = "South High",
                Discriminator = "School",
                LastRefreshed = DateTime.UtcNow
            });
        context.SaveChanges();

        var query = new GetEducationOrganizationsQuery(context, DefaultOptions());

        var result = await query.ExecuteAsync();

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(1);
        result[0].EducationOrganizations.Count.ShouldBe(2);
        result[1].Id.ShouldBe(2);
        result[1].EducationOrganizations.Count.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_WithInstanceIdFilter_ReturnsOnlyMatchingGroup()
    {
        using var context = CreateContext();
        context.EducationOrganizations.AddRange(
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Sandbox A",
                EducationOrganizationId = 1001,
                NameOfInstitution = "North High",
                Discriminator = "School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 2,
                InstanceName = "Sandbox B",
                EducationOrganizationId = 2001,
                NameOfInstitution = "South High",
                Discriminator = "School",
                LastRefreshed = DateTime.UtcNow
            });
        context.SaveChanges();

        var query = new GetEducationOrganizationsQuery(context, DefaultOptions());

        var result = await query.ExecuteAsync(new CommonQueryParams(0, 25), 2);

        result.Count.ShouldBe(1);
        result.Single().Id.ShouldBe(2);
        result.Single().EducationOrganizations.Single().EducationOrganizationId.ShouldBe(2001);
    }
}
