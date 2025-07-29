// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using NUnit.Framework;
using Shouldly;
using EdFi.Ods.AdminApi.Features.ApiClients;
using System.Collections.Generic;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ApiClients
{
    [TestFixture]
    public class ApiClientModelTests
    {
        [Test]
        public void ApiClientModel_DefaultValues_AreSetCorrectly()
        {
            var model = new ApiClientModel();
            model.Id.ShouldBe(0);
            model.Key.ShouldBe(string.Empty);
            model.Name.ShouldBe(string.Empty);
            model.IsApproved.ShouldBeTrue();
            model.UseSandbox.ShouldBeFalse();
            model.SandboxType.ShouldBe(0);
            model.ApplicationId.ShouldBe(0);
            model.KeyStatus.ShouldBe("Active");
            model.EducationOrganizationIds.ShouldBeNull();
            model.OdsInstanceIds.ShouldBeNull();
        }

        [Test]
        public void ApiClientModel_SetProperties_ValuesAreSetCorrectly()
        {
            var model = new ApiClientModel
            {
                Id = 1,
                Key = "TestKey",
                Name = "TestName",
                IsApproved = false,
                UseSandbox = true,
                SandboxType = 2,
                ApplicationId = 99,
                KeyStatus = "Inactive",
                EducationOrganizationIds = new List<long> { 1001, 1002 },
                OdsInstanceIds = new List<int> { 1, 2 }
            };

            model.Id.ShouldBe(1);
            model.Key.ShouldBe("TestKey");
            model.Name.ShouldBe("TestName");
            model.IsApproved.ShouldBeFalse();
            model.UseSandbox.ShouldBeTrue();
            model.SandboxType.ShouldBe(2);
            model.ApplicationId.ShouldBe(99);
            model.KeyStatus.ShouldBe("Inactive");
            model.EducationOrganizationIds.ShouldBe(new List<long> { 1001, 1002 });
            model.OdsInstanceIds.ShouldBe(new List<int> { 1, 2 });
        }
    }

    [TestFixture]
    public class ApiClientResultTests
    {
        [Test]
        public void ApiClientResult_DefaultValues_AreSetCorrectly()
        {
            var result = new ApiClientResult();
            result.Id.ShouldBe(0);
            result.Name.ShouldBe(string.Empty);
            result.Key.ShouldBe(string.Empty);
            result.Secret.ShouldBe(string.Empty);
            result.ApplicationId.ShouldBe(0);
        }

        [Test]
        public void ApiClientResult_SetProperties_ValuesAreSetCorrectly()
        {
            var result = new ApiClientResult
            {
                Id = 5,
                Name = "ClientName",
                Key = "ClientKey",
                Secret = "ClientSecret",
                ApplicationId = 42
            };

            result.Id.ShouldBe(5);
            result.Name.ShouldBe("ClientName");
            result.Key.ShouldBe("ClientKey");
            result.Secret.ShouldBe("ClientSecret");
            result.ApplicationId.ShouldBe(42);
        }
    }
}
