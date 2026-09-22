// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Audit;

[TestFixture]
public class DeletedEntityAuditCaptureTests
{
    [Test]
    public void Record_WhenRegistryReturnsProjection_SetsCapturedJson()
    {
        var registry = A.Fake<IDeletedEntitySnapshotRegistry>();
        var entity = new object();
        A.CallTo(() => registry.Project(entity)).Returns(new { Key = "abc123" });
        var capture = new DeletedEntityAuditCapture(registry);

        capture.Record(entity);

        capture.CapturedJson.ShouldBe("{\"Key\":\"abc123\"}");
    }

    [Test]
    public void Record_CalledTwice_SecondCallIsNoOp()
    {
        var registry = A.Fake<IDeletedEntitySnapshotRegistry>();
        var first = new object();
        var second = new object();
        A.CallTo(() => registry.Project(first)).Returns(new { Key = "first" });
        A.CallTo(() => registry.Project(second)).Returns(new { Key = "second" });
        var capture = new DeletedEntityAuditCapture(registry);

        capture.Record(first);
        capture.Record(second);

        capture.CapturedJson.ShouldBe("{\"Key\":\"first\"}");
        A.CallTo(() => registry.Project(second)).MustNotHaveHappened();
    }

    [Test]
    public void Record_WithNullEntity_DoesNotLockInAndLeavesCapturedJsonNull()
    {
        var registry = A.Fake<IDeletedEntitySnapshotRegistry>();
        var capture = new DeletedEntityAuditCapture(registry);

        capture.Record(null);
        capture.CapturedJson.ShouldBeNull();

        var real = new object();
        A.CallTo(() => registry.Project(real)).Returns(new { Key = "real" });
        capture.Record(real);

        capture.CapturedJson.ShouldBe("{\"Key\":\"real\"}");
    }

    [Test]
    public void Record_WhenRegistryReturnsNull_LeavesCapturedJsonNullButLocksIn()
    {
        var registry = A.Fake<IDeletedEntitySnapshotRegistry>();
        var unregistered = new object();
        A.CallTo(() => registry.Project(unregistered)).Returns(null);
        var capture = new DeletedEntityAuditCapture(registry);

        capture.Record(unregistered);
        capture.CapturedJson.ShouldBeNull();

        var second = new object();
        capture.Record(second);

        A.CallTo(() => registry.Project(second)).MustNotHaveHappened();
    }

    [Test]
    public void Record_WhenRegistryThrows_SwallowsExceptionAndLeavesCapturedJsonNull()
    {
        var registry = A.Fake<IDeletedEntitySnapshotRegistry>();
        var entity = new object();
        A.CallTo(() => registry.Project(entity)).Throws<InvalidOperationException>();
        var capture = new DeletedEntityAuditCapture(registry);

        Should.NotThrow(() => capture.Record(entity));
        capture.CapturedJson.ShouldBeNull();
    }
}
