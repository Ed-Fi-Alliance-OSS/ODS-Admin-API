// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IAddDataStoreDerivativeCommand
{
    OdsInstanceDerivative Execute(IAddDataStoreDerivativeModel newDataStoreDerivative);
}

public class AddDataStoreDerivativeCommand : IAddDataStoreDerivativeCommand
{
    private readonly IUsersContext _context;

    public AddDataStoreDerivativeCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstanceDerivative Execute(IAddDataStoreDerivativeModel newDataStoreDerivative)
    {
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == newDataStoreDerivative.DataStoreId) ??
            throw new NotFoundException<int>("DataStore", newDataStoreDerivative.DataStoreId);

        var derivative = new OdsInstanceDerivative
        {
            DerivativeType = newDataStoreDerivative.DerivativeType,
            ConnectionString = newDataStoreDerivative.ConnectionString,
            OdsInstance = odsInstance
        };
        _context.OdsInstanceDerivatives.Add(derivative);
        _context.SaveChanges();
        return derivative;
    }
}

public interface IAddDataStoreDerivativeModel
{
    public int DataStoreId { get; set; }
    public string? DerivativeType { get; set; }
    public string? ConnectionString { get; set; }
}



