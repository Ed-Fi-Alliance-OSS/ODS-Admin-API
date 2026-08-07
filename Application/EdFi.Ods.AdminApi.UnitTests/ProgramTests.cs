// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests;

[TestFixture]
public class ProgramTests
{
    [Test]
    public void ShouldScheduleDataStoreManagementJobs_WhenFlagEnabled_ReturnsTrue()
    {
        var settings = new AppSettings { EnableDataStoreManagement = true };

        DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(settings).ShouldBeTrue();
    }

    [Test]
    public void ShouldScheduleDataStoreManagementJobs_WhenFlagDisabled_ReturnsFalse()
    {
        var settings = new AppSettings { EnableDataStoreManagement = false };

        DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(settings).ShouldBeFalse();
    }
}
