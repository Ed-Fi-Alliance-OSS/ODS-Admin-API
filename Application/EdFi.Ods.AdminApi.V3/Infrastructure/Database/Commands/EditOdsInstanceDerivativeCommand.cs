// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IEditDataStoreDerivativeCommand
{
    OdsInstanceDerivative Execute(IEditDataStoreDerivativeModel changedDataStoreDerivativeData);
}

public class EditDataStoreDerivativeCommand : IEditDataStoreDerivativeCommand
{
    private readonly IUsersContext _context;

    public EditDataStoreDerivativeCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstanceDerivative Execute(IEditDataStoreDerivativeModel changedDataStoreDerivativeData)
    {
        var odsInstance = _context.OdsInstances
            .SingleOrDefault(v => v.OdsInstanceId == changedDataStoreDerivativeData.DataStoreId) ??
            throw new NotFoundException<int>("dataStore", changedDataStoreDerivativeData.DataStoreId);
        var odsInstanceDerivative = _context.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .SingleOrDefault(v => v.OdsInstanceDerivativeId == changedDataStoreDerivativeData.Id) ??
            throw new NotFoundException<int>("dataStoreDerivative", changedDataStoreDerivativeData.Id);

        odsInstanceDerivative.DerivativeType = changedDataStoreDerivativeData.DerivativeType;
        odsInstanceDerivative.OdsInstance = odsInstance;

        _context.SaveChanges();
        return odsInstanceDerivative;
    }
}

public interface IEditDataStoreDerivativeModel
{
    public int Id { get; set; }
    public int DataStoreId { get; set; }
    public string? DerivativeType { get; set; }
}



