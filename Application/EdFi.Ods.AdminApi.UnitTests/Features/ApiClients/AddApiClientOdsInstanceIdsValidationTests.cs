// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Features.ApiClients;
using FakeItEasy;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ApiClients
{
    [TestFixture]
    public class AddApiClientOdsInstanceIdsValidationTests
    {
        private IUsersContext _fakeDb;
        private MethodInfo _validateMethod;

        [SetUp]
        public void SetUp()
        {
            _fakeDb = A.Fake<IUsersContext>();
            _validateMethod = typeof(AddApiClient)
                .GetMethod("ValidateOdsInstanceIds", BindingFlags.NonPublic | BindingFlags.Static);
        }

        private AddApiClient.AddApiClientRequest MakeRequest(IEnumerable<int> ids) => new AddApiClient.AddApiClientRequest
        {
            Name = "Test",
            ApplicationId = 1,
            OdsInstanceIds = ids
        };

        private void SetupOdsInstances(params int[] instanceIds)
        {
            var data = instanceIds.Select(id => new OdsInstance { OdsInstanceId = id }).ToList();
            var queryable = data.AsQueryable();

            var fakeDbSet = A.Fake<DbSet<OdsInstance>>(options => options.Implements(typeof(IQueryable<OdsInstance>)));
            A.CallTo(() => ((IQueryable<OdsInstance>)fakeDbSet).Provider).Returns(queryable.Provider);
            A.CallTo(() => ((IQueryable<OdsInstance>)fakeDbSet).Expression).Returns(queryable.Expression);
            A.CallTo(() => ((IQueryable<OdsInstance>)fakeDbSet).ElementType).Returns(queryable.ElementType);
            A.CallTo(() => ((IQueryable<OdsInstance>)fakeDbSet).GetEnumerator()).Returns(queryable.GetEnumerator());

            A.CallTo(() => _fakeDb.OdsInstances).Returns(fakeDbSet);
        }

        [Test]
        public void Throws_When_OdsInstanceIds_NotInDatabase()
        {
            SetupOdsInstances(1);
            var request = MakeRequest(new[] { 2, 3 });

            var ex = Should.Throw<TargetInvocationException>(() =>
                _validateMethod.Invoke(null, new object[] { request, _fakeDb })
            );
            ex.InnerException.ShouldBeOfType<ValidationException>();
            ex.InnerException.Message.ShouldContain("not found in database");
        }

        [Test]
        public void Throws_When_OdsInstanceIds_Provided_But_None_In_Database()
        {
            SetupOdsInstances(); // Empty
            var request = MakeRequest(new[] { 1 });

            var ex = Should.Throw<TargetInvocationException>(() =>
                _validateMethod.Invoke(null, new object[] { request, _fakeDb })
            );
            ex.InnerException.ShouldBeOfType<ValidationException>();
            ex.InnerException.Message.ShouldContain("not found in database");
        }

        [Test]
        public void Does_Not_Throw_When_All_OdsInstanceIds_Exist()
        {
            SetupOdsInstances(1, 2);
            var request = MakeRequest(new[] { 1, 2 });

            Should.NotThrow(() =>
                _validateMethod.Invoke(null, new object[] { request, _fakeDb })
            );
        }

        [Test]
        public void Does_Not_Throw_When_OdsInstanceIds_Is_Null()
        {
            SetupOdsInstances(1);
            var request = new AddApiClient.AddApiClientRequest
            {
                Name = "Test",
                ApplicationId = 1,
                OdsInstanceIds = null
            };

            Should.NotThrow(() =>
                _validateMethod.Invoke(null, new object[] { request, _fakeDb })
            );
        }

        [Test]
        public void Does_Not_Throw_When_OdsInstanceIds_Is_Empty()
        {
            SetupOdsInstances(1);
            var request = MakeRequest(System.Array.Empty<int>());

            Should.NotThrow(() =>
                _validateMethod.Invoke(null, new object[] { request, _fakeDb })
            );
        }

        // Add the TearDown method to properly dispose of the _fakeDb field.

        [TearDown]
        public void TearDown()
        {
            _fakeDb?.Dispose();
        }
    }
}
