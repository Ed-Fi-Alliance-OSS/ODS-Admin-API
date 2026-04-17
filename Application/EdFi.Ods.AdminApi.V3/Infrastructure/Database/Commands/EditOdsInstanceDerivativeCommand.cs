// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IEditOdsInstanceDerivativeCommand
{
    OdsInstanceDerivative Execute(IEditOdsInstanceDerivativeModel changedOdsInstanceDerivativeData);
}

public class EditOdsInstanceDerivativeCommand : IEditOdsInstanceDerivativeCommand
{
    private readonly IUsersContext _context;

    public EditOdsInstanceDerivativeCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstanceDerivative Execute(IEditOdsInstanceDerivativeModel changedOdsInstanceDerivativeData)
    {
        var odsInstance = _context.OdsInstances
            .SingleOrDefault(v => v.OdsInstanceId == changedOdsInstanceDerivativeData.OdsInstanceId) ??
            throw new NotFoundException<int>("odsInstance", changedOdsInstanceDerivativeData.OdsInstanceId);
        var odsInstanceDerivative = _context.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .SingleOrDefault(v => v.OdsInstanceDerivativeId == changedOdsInstanceDerivativeData.Id) ??
            throw new NotFoundException<int>("odsInstanceDerivative", changedOdsInstanceDerivativeData.Id);

        odsInstanceDerivative.DerivativeType = changedOdsInstanceDerivativeData.DerivativeType;
        odsInstanceDerivative.OdsInstance = odsInstance;
        odsInstanceDerivative.ConnectionString = changedOdsInstanceDerivativeData.ConnectionString;

        _context.SaveChanges();
        return odsInstanceDerivative;
    }
}

public interface IEditOdsInstanceDerivativeModel
{
    public int Id { get; set; }
    public int OdsInstanceId { get; set; }
    public string? DerivativeType { get; set; }
    public string? ConnectionString { get; set; }
}
