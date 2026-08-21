// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores.Manage;

[TestFixture]
public class DataStoreManageDatabaseNameFormatterTests
{
    [TestCase("Minimal", 63)]
    [TestCase("Sample", 62)]
    public void Build_WithNameAtDataStoreManageMaxLength_NeverExceedsPortableLimit(string databaseTemplate, int expectedLength)
    {
        // AddDataStoreManage.Validator caps Name at 46 characters specifically so that, for
        // either DatabaseTemplate value, the generated DatabaseName can never exceed the
        // 63-character portable limit below.
        var databaseName = DataStoreManageDatabaseNameFormatter.Build(new string('a', 46), databaseTemplate);

        databaseName.Length.ShouldBe(expectedLength);
        databaseName.Length.ShouldBeLessThanOrEqualTo(DataStoreManageDatabaseNameFormatter.MaxPortableDatabaseNameLength);
    }

    [Test]
    public void Build_WithNameOverDataStoreManageMaxLength_ExceedsPortableLimit()
    {
        // Documents why AddDataStoreManage.Validator keeps its own "generated database name
        // exceeds the portable limit" check as defense-in-depth: a Name longer than the 46-char
        // field limit — such as one that reached this formatter without passing through the
        // validator — can produce a DatabaseName over the 63-character portable limit.
        var databaseName = DataStoreManageDatabaseNameFormatter.Build(new string('a', 47), "Minimal");

        databaseName.Length.ShouldBeGreaterThan(DataStoreManageDatabaseNameFormatter.MaxPortableDatabaseNameLength);
    }
}
