// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Features.Vendors;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Vendors
{
    [TestFixture]
    public class ReadVendorTests
    {
        [Test]
        public async Task GetVendors_ReturnsOkWithMappedList()
        {
            var fakeQuery = A.Fake<IGetVendorsQuery>();
            var fakeMapper = A.Fake<IMapper>();
            var queryParams = new CommonQueryParams(0, 10);
            var queryResult = new List<Vendor> { new Vendor { VendorId = 1, VendorName = "Acme" } };
            var mappedResult = new List<VendorModel> { new VendorModel { Id = 1, Company = "Acme" } };

            A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null, null, null, null)).Returns(queryResult);
            A.CallTo(() => fakeMapper.Map<List<VendorModel>>(queryResult)).Returns(mappedResult);

            var result = await ReadVendor.GetVendors(fakeQuery, fakeMapper, queryParams, null, null, null, null, null);

            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<VendorModel>>>();
            var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<VendorModel>>;
            okResult!.Value.ShouldBe(mappedResult);
        }

        [Test]
        public async Task GetVendor_ReturnsOkWithMappedModel()
        {
            var fakeQuery = A.Fake<IGetVendorByIdQuery>();
            var fakeMapper = A.Fake<IMapper>();
            var queryResult = new Vendor { VendorId = 7, VendorName = "Acme" };
            var mappedResult = new VendorModel { Id = 7, Company = "Acme" };

            A.CallTo(() => fakeQuery.Execute(7)).Returns(queryResult);
            A.CallTo(() => fakeMapper.Map<VendorModel>(queryResult)).Returns(mappedResult);

            var result = await ReadVendor.GetVendor(fakeQuery, fakeMapper, 7);

            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<VendorModel>>();
            var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<VendorModel>;
            okResult!.Value.ShouldBe(mappedResult);
        }

        [Test]
        public void GetVendor_WhenNotFound_ThrowsNotFoundException()
        {
            var fakeQuery = A.Fake<IGetVendorByIdQuery>();
            var fakeMapper = A.Fake<IMapper>();

            A.CallTo(() => fakeQuery.Execute(99)).Returns(null);

            Should.Throw<NotFoundException<int>>(() => ReadVendor.GetVendor(fakeQuery, fakeMapper, 99).GetAwaiter().GetResult());
        }

        [Test]
        public void GetVendors_WhenQueryThrows_ExceptionIsPropagated()
        {
            var fakeQuery = A.Fake<IGetVendorsQuery>();
            var fakeMapper = A.Fake<IMapper>();

            A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null, null, null, null))
                .Throws(new System.Exception("Query failed"));

            Should.Throw<System.Exception>(async () => await ReadVendor.GetVendors(fakeQuery, fakeMapper, new CommonQueryParams(0, 10), null, null, null, null, null));
        }
    }
}
