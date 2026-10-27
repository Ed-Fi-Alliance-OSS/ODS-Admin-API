// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class ClientEducationOrganizationScopeResolverTests
{
    private static readonly long[] ApplicationIds = [1, 2, 3];

    private static ClientEducationOrganizationScope Resolve(
        long[]? ids,
        bool? inherits,
        ClientEducationOrganizationScope? current = null,
        bool isMove = false)
        => ClientEducationOrganizationScopeResolver.Resolve(ids, inherits, ApplicationIds, current, isMove);

    [Test]
    public void Resolve_ListNoFlag_IsNarrowed()
    {
        var result = Resolve([2], null);

        result.Inherits.ShouldBeFalse();
        result.EducationOrganizationIds.ShouldBe([2L], ignoreOrder: true);
    }

    [Test]
    public void Resolve_FalseWithList_IsNarrowed()
    {
        var result = Resolve([1, 3], false);

        result.Inherits.ShouldBeFalse();
        result.EducationOrganizationIds.ShouldBe([1L, 3L], ignoreOrder: true);
    }

    [Test]
    public void Resolve_TrueNoList_Inherits()
    {
        var result = Resolve(null, true);

        result.Inherits.ShouldBeTrue();
        result.EducationOrganizationIds.ShouldBe([1L, 2L, 3L], ignoreOrder: true);
    }

    [Test]
    public void Resolve_TrueWithEqualList_InheritsAndIgnoresOrder()
    {
        var result = Resolve([3, 1, 2], true);

        result.Inherits.ShouldBeTrue();
        result.EducationOrganizationIds.ShouldBe([1L, 2L, 3L], ignoreOrder: true);
    }

    [Test]
    public void Resolve_TrueWithOtherList_ThrowsBadRequestDataOnFlag()
    {
        var ex = Should.Throw<BadRequestDataException>(() => Resolve([1, 2], true));

        ex.PropertyName.ShouldBe("inheritsApplicationEducationOrganizations");
    }

    [Test]
    public void Resolve_FalseNoList_ThrowsBadRequestDataOnList()
    {
        var ex = Should.Throw<BadRequestDataException>(() => Resolve(null, false));

        ex.PropertyName.ShouldBe("educationOrganizationIds");
    }

    [Test]
    public void Resolve_EmptyList_ThrowsBadRequestDataOnList()
    {
        var ex = Should.Throw<BadRequestDataException>(() => Resolve([], null));

        ex.PropertyName.ShouldBe("educationOrganizationIds");
    }

    [Test]
    public void Resolve_IdNotOnApplication_ThrowsBadRequestDataListingIt()
    {
        var ex = Should.Throw<BadRequestDataException>(() => Resolve([2, 9], null));

        ex.PropertyName.ShouldBe("educationOrganizationIds");
        ex.Message.ShouldContain("9");
    }

    [Test]
    public void Resolve_NeitherFieldPost_Inherits()
    {
        var result = Resolve(null, null);

        result.Inherits.ShouldBeTrue();
        result.EducationOrganizationIds.ShouldBe([1L, 2L, 3L], ignoreOrder: true);
    }

    [Test]
    public void Resolve_NeitherFieldPutSameApplication_KeepsNarrowed()
    {
        var result = Resolve(null, null, new ClientEducationOrganizationScope(false, [2]));

        result.Inherits.ShouldBeFalse();
        result.EducationOrganizationIds.ShouldBe([2L], ignoreOrder: true);
    }

    [Test]
    public void Resolve_NeitherFieldPutSameApplication_KeepsInheriting()
    {
        var result = Resolve(null, null, new ClientEducationOrganizationScope(true, [1, 2]));

        result.Inherits.ShouldBeTrue();
        result.EducationOrganizationIds.ShouldBe([1L, 2L, 3L], ignoreOrder: true);
    }

    [Test]
    public void Resolve_NeitherFieldMoveInheriting_TakesNewList()
    {
        var result = Resolve(null, null, new ClientEducationOrganizationScope(true, [8, 9]), isMove: true);

        result.Inherits.ShouldBeTrue();
        result.EducationOrganizationIds.ShouldBe([1L, 2L, 3L], ignoreOrder: true);
    }

    [Test]
    public void Resolve_NeitherFieldMoveNarrowedFits_KeepsList()
    {
        var result = Resolve(null, null, new ClientEducationOrganizationScope(false, [2, 3]), isMove: true);

        result.Inherits.ShouldBeFalse();
        result.EducationOrganizationIds.ShouldBe([2L, 3L], ignoreOrder: true);
    }

    [Test]
    public void Resolve_NeitherFieldMoveNarrowedDoesNotFit_ThrowsScopeConflict()
    {
        var ex = Should.Throw<ConflictException>(
            () => Resolve(null, null, new ClientEducationOrganizationScope(false, [2, 7]), isMove: true));

        ex.ProblemType.ShouldBe(AdminApiProblemTypes.ConflictEducationOrganizationScope);
        ex.Message.ShouldContain("7");
    }

    [Test]
    public void Resolve_ListWithDuplicates_IsDeduplicated()
    {
        var result = Resolve([2, 2], null);

        result.EducationOrganizationIds.ShouldBe([2L], ignoreOrder: true);
    }
}
