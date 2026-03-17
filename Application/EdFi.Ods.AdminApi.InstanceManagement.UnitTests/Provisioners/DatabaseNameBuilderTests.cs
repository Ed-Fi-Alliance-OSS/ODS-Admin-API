// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
using Shouldly;

namespace EdFi.Ods.AdminApi.InstanceManagement.UnitTests.Provisioners;

[TestFixture]
public class DatabaseNameBuilderTests
{
    [TestCase("EdFi_Ods")]
    public void DemoSandboxDatabase_ShouldReturnExpectedValue(string expectedDatabaseName)
    {
        var sut = new DatabaseNameBuilder();

        sut.DemoSandboxDatabase.ShouldBe(expectedDatabaseName);
    }

    [TestCase("Ods_Empty_Template")]
    public void EmptyDatabase_ShouldReturnExpectedValue(string expectedDatabaseName)
    {
        var sut = new DatabaseNameBuilder();

        sut.EmptyDatabase.ShouldBe(expectedDatabaseName);
    }

    [TestCase("Ods_Minimal_Template")]
    public void MinimalDatabase_ShouldReturnExpectedValue(string expectedDatabaseName)
    {
        var sut = new DatabaseNameBuilder();

        sut.MinimalDatabase.ShouldBe(expectedDatabaseName);
    }

    [TestCase("Ods_Populated_Template")]
    public void SampleDatabase_ShouldReturnExpectedValue(string expectedDatabaseName)
    {
        var sut = new DatabaseNameBuilder();

        sut.SampleDatabase.ShouldBe(expectedDatabaseName);
    }
}
