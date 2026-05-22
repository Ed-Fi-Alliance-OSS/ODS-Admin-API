// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Features.OdsInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Quartz;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances;

[TestFixture]
public class RefreshEducationOrganizationsTests
{
    private IScheduler _scheduler = null!;
    private IContextProvider<TenantConfiguration> _tenantConfigurationProvider = null!;
    private IGetOdsInstanceQuery _getOdsInstanceByIdQuery = null!;
    private ISchedulerFactory _schedulerFactory = null!;
    private IJobStatusService _jobStatusService = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduler = A.Fake<IScheduler>();
        _tenantConfigurationProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        _getOdsInstanceByIdQuery = A.Fake<IGetOdsInstanceQuery>();
        _schedulerFactory = new TestSchedulerFactory(_scheduler);
        _jobStatusService = A.Fake<IJobStatusService>();
    }

    [Test]
    public async Task RefreshAllEducationOrganizations_ReturnsCreatedResult()
    {
        // Arrange
        var tenantConfiguration = new TenantConfiguration { TenantIdentifier = "EdFi_Admin" };
        A.CallTo(() => _tenantConfigurationProvider.Get()).Returns(tenantConfiguration);
        A.CallTo(() => _scheduler.SchedulerInstanceId).Returns("QuartzWorkerPool:localhost.localdomain1-1234567890");

        // Act
        var result = await RefreshEducationOrganizations.RefreshAllEducationOrganizations(
            _schedulerFactory,
            _tenantConfigurationProvider,
            _jobStatusService);

        // Assert
        result.ShouldNotBeNull();
        // Verify the handler was called and returned something
        var resultType = result.GetType().Name;
        resultType.ShouldContain("Created");
    }

    [Test]
    public async Task RefreshEducationOrganizationsByInstance_ReturnsCreatedResult()
    {
        // Arrange
        var instanceId = 1;
        var odsInstance = new OdsInstance { OdsInstanceId = instanceId, Name = "TestInstance" };
        A.CallTo(() => _getOdsInstanceByIdQuery.Execute(instanceId)).Returns(odsInstance);

        var tenantConfiguration = new TenantConfiguration { TenantIdentifier = "EdFi_Admin" };
        A.CallTo(() => _tenantConfigurationProvider.Get()).Returns(tenantConfiguration);
        A.CallTo(() => _scheduler.SchedulerInstanceId).Returns("QuartzWorkerPool:localhost.localdomain1-1234567890");

        // Act
        var result = await RefreshEducationOrganizations.RefreshEducationOrganizationsByInstance(
            _schedulerFactory,
            _getOdsInstanceByIdQuery,
            _tenantConfigurationProvider,
            _jobStatusService,
            instanceId);

        // Assert
        result.ShouldNotBeNull();
        // Verify the handler was called and returned something
        var resultType = result.GetType().Name;
        resultType.ShouldContain("Created");
    }

    private class TestSchedulerFactory : ISchedulerFactory
    {
        private readonly IScheduler _scheduler;

        public TestSchedulerFactory(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public Task<IScheduler> GetScheduler()
        {
            return Task.FromResult(_scheduler);
        }

        public Task<IScheduler> GetScheduler(CancellationToken cancellationToken)
        {
            return Task.FromResult(_scheduler);
        }

        public Task<IScheduler> GetScheduler(string schedName)
        {
            return Task.FromResult(_scheduler);
        }

        public Task<IScheduler> GetScheduler(string schedName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_scheduler);
        }

        public Task<IScheduler> GetScheduler(string schedName, string instId)
        {
            return Task.FromResult(_scheduler);
        }

        public Task<IReadOnlyList<IScheduler>> GetAllSchedulers()
        {
            return Task.FromResult<IReadOnlyList<IScheduler>>(new List<IScheduler> { _scheduler });
        }

        public Task<IReadOnlyList<IScheduler>> GetAllSchedulers(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<IScheduler>>(new List<IScheduler> { _scheduler });
        }
    }
}
