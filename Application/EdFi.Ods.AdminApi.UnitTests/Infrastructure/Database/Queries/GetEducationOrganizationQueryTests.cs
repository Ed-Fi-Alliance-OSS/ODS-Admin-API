// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetEducationOrganizationQueryTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"GetEducationOrganizationQueryTests_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "Postgres"
            })
            .Build();

        return new AdminApiDbContext(options, configuration);
    }

    [Test]
    public void Execute_WithInstanceId_ReturnsMatchingEducationOrganizations()
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

        var query = new GetEducationOrganizationQuery(context);

        var result = query.Execute(1);

        result.Count.ShouldBe(1);
        result.Single().EducationOrganizationId.ShouldBe(1001);
    }

    [Test]
    public void Execute_WithMultipleInstanceIds_ReturnsMatchingEducationOrganizations()
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
            },
            new EducationOrganization
            {
                InstanceId = 3,
                InstanceName = "Sandbox C",
                EducationOrganizationId = 3001,
                NameOfInstitution = "West High",
                Discriminator = "School",
                LastRefreshed = DateTime.UtcNow
            });
        context.SaveChanges();

        var query = new GetEducationOrganizationQuery(context);

        var result = query.Execute([1, 3]);

        result.Count.ShouldBe(2);
        result.Select(x => x.EducationOrganizationId).ShouldBe([1001L, 3001L], ignoreOrder: true);
    }
}
