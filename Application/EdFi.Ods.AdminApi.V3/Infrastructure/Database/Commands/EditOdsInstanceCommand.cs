// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IEditDataStoreCommand
{
    OdsInstance Execute(IEditDataStoreModel changedDataStore);
}

public class EditDataStoreCommand : IEditDataStoreCommand
{
    private readonly IUsersContext _context;

    public EditDataStoreCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstance Execute(IEditDataStoreModel changedDataStore)
    {
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == changedDataStore.Id) ??
            throw new NotFoundException<int>("dataStore", changedDataStore.Id);

        odsInstance.Name = changedDataStore.Name;
        odsInstance.InstanceType = changedDataStore.DataStoreType;
        if (!string.IsNullOrEmpty(changedDataStore.ConnectionString))
            odsInstance.ConnectionString = changedDataStore.ConnectionString;

        _context.SaveChanges();
        return odsInstance;
    }
}

public interface IEditDataStoreModel
{
    public int Id { get; set; }
    string? Name { get; }
    string? DataStoreType { get; }
    string? ConnectionString { get; }
}




