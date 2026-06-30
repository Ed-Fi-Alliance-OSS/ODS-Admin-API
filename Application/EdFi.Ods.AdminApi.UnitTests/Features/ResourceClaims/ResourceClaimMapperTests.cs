// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Features.ResourceClaims;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ResourceClaims;

[TestFixture]
public class ResourceClaimMapperTests
{
    [Test]
    public void ToModel_MapsResourceClaimValuesAndChildren()
    {
        var source = new ResourceClaim
        {
            Id = 1,
            Name = "Parent",
            ParentId = 10,
            ParentName = "GrandParent",
            Children = new List<ResourceClaim>
            {
                new()
                {
                    Id = 2,
                    Name = "Child",
                    ParentId = 1,
                    ParentName = "Parent"
                }
            }
        };

        var model = ResourceClaimMapper.ToModel(source);

        model.Id.ShouldBe(1);
        model.Name.ShouldBe("Parent");
        model.ParentId.ShouldBe(10);
        model.ParentName.ShouldBe("GrandParent");
        model.Children.Single().Id.ShouldBe(2);
        model.Children.Single().Name.ShouldBe("Child");
        model.Children.Single().ParentId.ShouldBe(1);
        model.Children.Single().ParentName.ShouldBe("Parent");
    }

    [Test]
    public void ToModelList_MapsAllResourceClaimsInOrder()
    {
        var source = new[]
        {
            new ResourceClaim { Id = 1, Name = "First" },
            new ResourceClaim { Id = 2, Name = "Second" }
        };

        var models = ResourceClaimMapper.ToModelList(source);

        models.Select(x => x.Id).ShouldBe(new[] { 1, 2 });
        models.Select(x => x.Name).ShouldBe(new[] { "First", "Second" });
    }
}
