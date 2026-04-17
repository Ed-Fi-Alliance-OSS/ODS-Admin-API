// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V2.Infrastructure.Database.Commands;

public interface IAddOdsInstanceDerivativeCommand
{
    OdsInstanceDerivative Execute(IAddOdsInstanceDerivativeModel newOdsInstanceDerivative);
}

public class AddOdsInstanceDerivativeCommand : IAddOdsInstanceDerivativeCommand
{
    private readonly IUsersContext _context;

    public AddOdsInstanceDerivativeCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstanceDerivative Execute(IAddOdsInstanceDerivativeModel newOdsInstanceDerivative)
    {
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == newOdsInstanceDerivative.OdsInstanceId) ??
            throw new NotFoundException<int>("odsInstance", newOdsInstanceDerivative.OdsInstanceId);

        var odsInstanceDerivative = new OdsInstanceDerivative
        {
           ConnectionString = newOdsInstanceDerivative.ConnectionString,
           DerivativeType = newOdsInstanceDerivative.DerivativeType,
           OdsInstance = odsInstance
        };
        _context.OdsInstanceDerivatives.Add(odsInstanceDerivative);
        _context.SaveChanges();
        return odsInstanceDerivative;
    }
}

public interface IAddOdsInstanceDerivativeModel
{
    public int OdsInstanceId { get; set; }
    public string? DerivativeType { get; set; }
    public string? ConnectionString { get; set; }
}
