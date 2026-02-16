// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.AutoMapper;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetEducationOrganizationsQueryTests : AdminApiDbContextTestBase
{
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUpMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AdminApiMappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Test]
    public async Task ShouldGetAllEducationOrganizations()
    {
        await Transaction(async context =>
        {
            CreateMultiple(context);
            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync();
            var results = instances.SelectMany(i => i.EducationOrganizations).ToList();
            results.Count.ShouldBe(5);
        });
    }

    [Test]
    public async Task ShouldGetEducationOrganizationsWithOffsetAndLimit()
    {
        await Transaction(async context =>
        {
            CreateMultiple(context);
            var offset = 0;
            var limit = 2;

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(new CommonQueryParams(offset, limit), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(2);

            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 1");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 2");

            offset = 2;

            instances = await query.ExecuteAsync(new CommonQueryParams(offset, limit), null);
            educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(2);
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 3");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 4");

            offset = 4;

            instances = await query.ExecuteAsync(new CommonQueryParams(offset, limit), null);
            educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(1);
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 5");
        });
    }

    [Test]
    public async Task ShouldGetEducationOrganizationsWithoutOffsetAndLimit()
    {
        await Transaction(async context =>
        {
            CreateMultiple(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(new CommonQueryParams(), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(5);

            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 1");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 2");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 3");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 4");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 5");
        });
    }

    [Test]
    public async Task ShouldGetEducationOrganizationsWithoutLimit()
    {
        await Transaction(async context =>
        {
            CreateMultiple(context);
            var offset = 0;

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(new CommonQueryParams(offset, null), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(5);

            offset = 2;

            instances = await query.ExecuteAsync(new CommonQueryParams(offset, null), null);
            educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(3);
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 3");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 4");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 5");

            offset = 4;

            instances = await query.ExecuteAsync(new CommonQueryParams(offset, null), null);
            educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(1);
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 5");
        });
    }

    [Test]
    public async Task ShouldGetEducationOrganizationsWithoutOffset()
    {
        await Transaction(async context =>
        {
            CreateMultiple(context);
            var limit = 2;

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(new CommonQueryParams(null, limit), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(2);

            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 1");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Test School 2");
        });
    }

    [Test]
    public async Task ShouldGetEducationOrganizationsByInstanceId()
    {
        await Transaction(async context =>
        {
            CreateMultipleForDifferentInstances(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(new CommonQueryParams(), 1);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(3);

            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Instance 1 School 1");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Instance 1 School 2");
            educationOrganizations.ShouldContain(eo => eo.NameOfInstitution == "Instance 1 School 3");
        });
    }

    [Test]
    public async Task ShouldReturnEmptyListForNonExistentInstanceId()
    {
        await Transaction(async context =>
        {
            CreateMultipleForDifferentInstances(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(new CommonQueryParams(), 999);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldBeEmpty();
        });
    }

    [Test]
    public async Task ShouldOrderByEducationOrganizationIdAscending()
    {
        await Transaction(async context =>
        {
            CreateMultipleWithDifferentIds(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(
                new CommonQueryParams(null, null, "educationOrganizationId", "asc"), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(5);
            educationOrganizations[0].EducationOrganizationId.ShouldBe(100);
            educationOrganizations[1].EducationOrganizationId.ShouldBe(200);
            educationOrganizations[2].EducationOrganizationId.ShouldBe(300);
            educationOrganizations[3].EducationOrganizationId.ShouldBe(400);
            educationOrganizations[4].EducationOrganizationId.ShouldBe(500);
        });
    }

    [Test]
    public async Task ShouldOrderByEducationOrganizationIdDescending()
    {
        await Transaction(async context =>
        {
            CreateMultipleWithDifferentIds(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(
                new CommonQueryParams(null, null, "educationOrganizationId", "desc"), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(5);
            educationOrganizations[0].EducationOrganizationId.ShouldBe(500);
            educationOrganizations[1].EducationOrganizationId.ShouldBe(400);
            educationOrganizations[2].EducationOrganizationId.ShouldBe(300);
            educationOrganizations[3].EducationOrganizationId.ShouldBe(200);
            educationOrganizations[4].EducationOrganizationId.ShouldBe(100);
        });
    }

    [Test]
    public async Task ShouldOrderByNameOfInstitutionAscending()
    {
        await Transaction(async context =>
        {
            CreateMultipleWithDifferentNames(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(
                new CommonQueryParams(null, null, "nameOfInstitution", "asc"), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(5);
            educationOrganizations[0].NameOfInstitution.ShouldBe("Alpha School");
            educationOrganizations[1].NameOfInstitution.ShouldBe("Beta School");
            educationOrganizations[2].NameOfInstitution.ShouldBe("Charlie School");
            educationOrganizations[3].NameOfInstitution.ShouldBe("Delta School");
            educationOrganizations[4].NameOfInstitution.ShouldBe("Echo School");
        });
    }

    [Test]
    public async Task ShouldOrderByNameOfInstitutionDescending()
    {
        await Transaction(async context =>
        {
            CreateMultipleWithDifferentNames(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(
                new CommonQueryParams(null, null, "nameOfInstitution", "desc"), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(5);
            educationOrganizations[0].NameOfInstitution.ShouldBe("Echo School");
            educationOrganizations[1].NameOfInstitution.ShouldBe("Delta School");
            educationOrganizations[2].NameOfInstitution.ShouldBe("Charlie School");
            educationOrganizations[3].NameOfInstitution.ShouldBe("Beta School");
            educationOrganizations[4].NameOfInstitution.ShouldBe("Alpha School");
        });
    }

    [Test]
    public async Task ShouldOrderByDiscriminatorAscending()
    {
        await Transaction(async context =>
        {
            CreateMultipleWithDifferentDiscriminators(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(
                new CommonQueryParams(null, null, "discriminator", "asc"), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(4);
            educationOrganizations[0].Discriminator.ShouldBe("edfi.LocalEducationAgency");
            educationOrganizations[1].Discriminator.ShouldBe("edfi.School");
            educationOrganizations[2].Discriminator.ShouldBe("edfi.School");
            educationOrganizations[3].Discriminator.ShouldBe("edfi.StateEducationAgency");
        });
    }

    [Test]
    public async Task ShouldOrderByInstanceIdAscending()
    {
        await Transaction(async context =>
        {
            CreateMultipleForDifferentInstances(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(
                new CommonQueryParams(null, null, "instanceId", "asc"), null);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(5);
            educationOrganizations[0].EducationOrganizationId.ShouldBe(1001);
            educationOrganizations[1].EducationOrganizationId.ShouldBe(1002);
            educationOrganizations[2].EducationOrganizationId.ShouldBe(1003);
            educationOrganizations[3].EducationOrganizationId.ShouldBe(2001);
            educationOrganizations[4].EducationOrganizationId.ShouldBe(2002);
        });
    }

    [Test]
    public async Task ShouldCombineInstanceIdFilterWithPagination()
    {
        await Transaction(async context =>
        {
            CreateMultipleForDifferentInstances(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(new CommonQueryParams(offset: 1, limit: 1), 1);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(1);
            educationOrganizations[0].NameOfInstitution.ShouldBe("Instance 1 School 2");
        });
    }

    [Test]
    public async Task ShouldCombineInstanceIdFilterWithOrdering()
    {
        await Transaction(async context =>
        {
            CreateMultipleForDifferentInstancesWithDifferentIds(context);

            var query = new GetEducationOrganizationsQuery(context, Testing.GetAppSettings(), _mapper);
            var instances = await query.ExecuteAsync(
                new CommonQueryParams(null, null, "educationOrganizationId", "desc"), 1);
            var educationOrganizations = instances.SelectMany(i => i.EducationOrganizations).ToList();

            educationOrganizations.ShouldNotBeEmpty();
            educationOrganizations.Count.ShouldBe(3);
            educationOrganizations[0].EducationOrganizationId.ShouldBe(300);
            educationOrganizations[1].EducationOrganizationId.ShouldBe(200);
            educationOrganizations[2].EducationOrganizationId.ShouldBe(100);
        });
    }

    private static void CreateMultiple(AdminApiDbContext context, int total = 5)
    {
        for (var i = 0; i < total; i++)
        {
            context.EducationOrganizations.Add(new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1000 + i,
                NameOfInstitution = $"Test School {i + 1}",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            });
        }
        context.SaveChanges();
    }

    private static void CreateMultipleForDifferentInstances(AdminApiDbContext context)
    {
        var organizations = new[]
        {
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance 1",
                EducationOrganizationId = 1001,
                NameOfInstitution = "Instance 1 School 1",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance 1",
                EducationOrganizationId = 1002,
                NameOfInstitution = "Instance 1 School 2",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance 1",
                EducationOrganizationId = 1003,
                NameOfInstitution = "Instance 1 School 3",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 2,
                InstanceName = "Test Instance 2",
                EducationOrganizationId = 2001,
                NameOfInstitution = "Instance 2 School 1",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 2,
                InstanceName = "Test Instance 2",
                EducationOrganizationId = 2002,
                NameOfInstitution = "Instance 2 School 2",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            }
        };

        context.EducationOrganizations.AddRange(organizations);
        context.SaveChanges();
    }

    private static void CreateMultipleWithDifferentIds(AdminApiDbContext context)
    {
        var organizations = new[]
        {
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 300,
                NameOfInstitution = "School C",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 100,
                NameOfInstitution = "School A",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 500,
                NameOfInstitution = "School E",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 200,
                NameOfInstitution = "School B",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 400,
                NameOfInstitution = "School D",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            }
        };

        context.EducationOrganizations.AddRange(organizations);
        context.SaveChanges();
    }

    private static void CreateMultipleWithDifferentNames(AdminApiDbContext context)
    {
        var organizations = new[]
        {
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1001,
                NameOfInstitution = "Charlie School",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1002,
                NameOfInstitution = "Alpha School",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1003,
                NameOfInstitution = "Echo School",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1004,
                NameOfInstitution = "Beta School",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1005,
                NameOfInstitution = "Delta School",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            }
        };

        context.EducationOrganizations.AddRange(organizations);
        context.SaveChanges();
    }

    private static void CreateMultipleWithDifferentDiscriminators(AdminApiDbContext context)
    {
        var organizations = new[]
        {
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1001,
                NameOfInstitution = "School 1",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1002,
                NameOfInstitution = "LEA 1",
                Discriminator = "edfi.LocalEducationAgency",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1003,
                NameOfInstitution = "School 2",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance",
                EducationOrganizationId = 1004,
                NameOfInstitution = "SEA 1",
                Discriminator = "edfi.StateEducationAgency",
                LastRefreshed = DateTime.UtcNow
            }
        };

        context.EducationOrganizations.AddRange(organizations);
        context.SaveChanges();
    }

    private static void CreateMultipleForDifferentInstancesWithDifferentIds(AdminApiDbContext context)
    {
        var organizations = new[]
        {
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance 1",
                EducationOrganizationId = 200,
                NameOfInstitution = "Instance 1 School 2",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance 1",
                EducationOrganizationId = 100,
                NameOfInstitution = "Instance 1 School 1",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 1,
                InstanceName = "Test Instance 1",
                EducationOrganizationId = 300,
                NameOfInstitution = "Instance 1 School 3",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 2,
                InstanceName = "Test Instance 2",
                EducationOrganizationId = 2001,
                NameOfInstitution = "Instance 2 School 1",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            },
            new EducationOrganization
            {
                InstanceId = 2,
                InstanceName = "Test Instance 2",
                EducationOrganizationId = 2002,
                NameOfInstitution = "Instance 2 School 2",
                Discriminator = "edfi.School",
                LastRefreshed = DateTime.UtcNow
            }
        };

        context.EducationOrganizations.AddRange(organizations);
        context.SaveChanges();
    }
}
