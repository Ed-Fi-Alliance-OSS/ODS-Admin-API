// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using NUnit.Framework;
using Shouldly;
using Application = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Application;
using ClaimSet = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.ClaimSet;
using VendorApplication = EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models.Application;

namespace EdFi.Ods.AdminApi.V1.DBTests.ClaimSetEditorTests;

[TestFixture]
public class GetApplicationsByClaimSetIdQueryServiceTests : SecurityDataTestBase
{
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(5)]
    public void ShouldGetApplicationsByClaimSetId(int applicationCount)
    {
        var testClaimSets = SetupApplicationWithClaimSets();

        SetupApplications(testClaimSets, applicationCount);
        using var securityContext = TestContext;

        foreach (var testClaimSet in testClaimSets)
        {
            UsersTransaction(usersContext =>
            {
                var query = new GetApplicationsByClaimSetIdQuery(securityContext, usersContext);
                var results = query.Execute(testClaimSet.ClaimSetId).ToList();

                var testApplications =
                    usersContext.Applications.Where(x => x.ClaimSetName == testClaimSet.ClaimSetName).Select(x => new Application
                    {
                        ApplicationName = x.ApplicationName,
                        ApplicationId = x.ApplicationId
                    }).ToArray();
                results.Count.ShouldBe(testApplications.Length);
                results.Select(x => x.Name).ShouldBe(testApplications.Select(x => x.ApplicationName), true);
            });
        }
    }


    [TestCase(1)]
    [TestCase(5)]
    public void ShouldGetClaimSetApplicationsCount(int applicationsCount)
    {
        var testClaimSets = SetupApplicationWithClaimSets();

        SetupApplications(testClaimSets, applicationsCount);

        using var securityContext = TestContext;
        foreach (var testClaimSet in testClaimSets)
        {
            UsersTransaction(usersContext =>
            {
                var query = new GetApplicationsByClaimSetIdQuery(securityContext, usersContext);
                var appsCountByClaimSet = query.ExecuteCount(testClaimSet.ClaimSetId);
                var testApplicationsCount =
                    usersContext.Applications.Count(x => x.ClaimSetName == testClaimSet.ClaimSetName);
                appsCountByClaimSet.ShouldBe(testApplicationsCount);
            });
        }
    }

    private IReadOnlyCollection<ClaimSet> SetupApplicationWithClaimSets(
        string applicationName = "TestApplicationName", int claimSetCount = 5)
    {
        var testApplication = new Application
        {
            ApplicationName = applicationName
        };

        Save(testApplication);

        var testClaimSetNames = Enumerable.Range(1, claimSetCount)
            .Select((x, index) => $"TestClaimSetName{index:N}")
            .ToArray();

        var testClaimSets = testClaimSetNames
            .Select(x => new ClaimSet
            {
                ClaimSetName = x,
                Application = testApplication
            })
            .ToArray();

        Save(testClaimSets.Cast<object>().ToArray());

        return testClaimSets;
    }

    private void SetupApplications(IEnumerable<ClaimSet> testClaimSets, int applicationCount = 5)
    {
        UsersTransaction(usersContext =>
        {
            foreach (var claimSet in testClaimSets)
            {
                foreach (var _ in Enumerable.Range(1, applicationCount))
                {
                    usersContext.Applications.Add(new VendorApplication
                    {
                        ApplicationName = $"TestAppVendorName{Guid.NewGuid():N}",
                        ClaimSetName = claimSet.ClaimSetName,
                        OperationalContextUri = OperationalContext.DefaultOperationalContextUri
                    });
                }
            }
            usersContext.SaveChanges();
        });
    }
}
