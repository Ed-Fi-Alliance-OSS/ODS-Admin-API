// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IAddDataStoreCommand
{
    OdsInstance Execute(IAddDataStoreModel newDataStore);
}

public class AddDataStoreCommand : IAddDataStoreCommand
{
    private readonly IUsersContext _context;

    public AddDataStoreCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstance Execute(IAddDataStoreModel newDataStore)
    {
        var odsInstance = new OdsInstance
        {
            Name = newDataStore.Name,
            InstanceType = newDataStore.DataStoreType,
            ConnectionString = newDataStore.ConnectionString
        };
        _context.OdsInstances.Add(odsInstance);
        _context.SaveChanges();
        return odsInstance;
    }
}

public interface IAddDataStoreModel
{
    string? Name { get; }
    string? DataStoreType { get; }
    string? ConnectionString { get; }
}

