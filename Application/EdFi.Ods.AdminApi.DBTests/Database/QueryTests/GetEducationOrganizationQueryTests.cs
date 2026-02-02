// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetEducationOrganizationQueryTests : AdminApiDbContextTestBase
{
    [Test]
    public void ShouldGetAllEducationOrganizations()
    {

        Transaction(adminApiDbContext =>
        {
            CreateMultiple(3);
            var command = new GetEducationOrganizationQuery(adminApiDbContext);
            var results = command.Execute();
            results.Count.ShouldBe(3);
        });
    }

    [Test]
    public void ShouldGetEducationOrganizationsByOdsInstanceId()
    {
        Transaction(adminApiDbContext =>
        {
            var educationOrganizations = CreateMultiple(5);
            var command = new GetEducationOrganizationQuery(adminApiDbContext);

            var resultsForInstance1 = command.Execute(educationOrganizations[0].InstanceId);

            resultsForInstance1.ShouldNotBeEmpty();
            resultsForInstance1.Count.ShouldBe(2);
            resultsForInstance1.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 1");
            resultsForInstance1.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 2");

            var resultsForInstance2 = command.Execute(2);

            resultsForInstance2.ShouldNotBeEmpty();
            resultsForInstance2.Count.ShouldBe(3);
            resultsForInstance2.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 3");
            resultsForInstance2.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 4");
            resultsForInstance2.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 5");
        });
    }

    [Test]
    public void ShouldReturnEmptyList_WhenNoEducationOrganizationsExist()
    {
        Transaction(adminApiDbContext =>
        {
            var command = new GetEducationOrganizationQuery(adminApiDbContext);
            var results = command.Execute();
            results.ShouldBeEmpty();
        });
    }

    [Test]
    public void ShouldReturnEmptyList_WhenOdsInstanceIdNotFound()
    {
        Transaction(adminApiDbContext =>
        {
            CreateMultiple(3);
            var command = new GetEducationOrganizationQuery(adminApiDbContext);

            var results = command.Execute(999);

            results.ShouldBeEmpty();
        });
    }

    [Test]
    public void ShouldGetAllEducationOrganizationsFromMultipleInstances()
    {
        Transaction(adminApiDbContext =>
        {
            CreateMultiple(5);
            var command = new GetEducationOrganizationQuery(adminApiDbContext);

            var allResults = command.Execute();

            allResults.ShouldNotBeEmpty();
            allResults.Count.ShouldBe(5);
            allResults.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 1");
            allResults.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 2");
            allResults.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 3");
            allResults.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 4");
            allResults.ShouldContain(edOrg => edOrg.NameOfInstitution == "Test School 5");
        });
    }

    private static EducationOrganization[] CreateMultiple(int total = 5)
    {
        var educationOrganizations = new EducationOrganization[total];

        for (var edOrgIndex = 0; edOrgIndex < total; edOrgIndex++)
        {
            educationOrganizations[edOrgIndex] = new EducationOrganization
            {
                InstanceId = edOrgIndex < 2 ? 1 : 2,
                EducationOrganizationId = 1000 + edOrgIndex,
                NameOfInstitution = $"Test School {edOrgIndex + 1}",
                ShortNameOfInstitution = $"TS{edOrgIndex + 1}",
                Discriminator = "School"
            };
        }
        Save(educationOrganizations);

        return educationOrganizations;
    }
}
