// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;

public interface IBaseRepository
{
    void SwitchConnectionString(string connectionString);
    void ResetConnectionString();
    Task SaveChangesAsync();
}

public abstract class BaseRepository<T> : IBaseRepository where T : class
{
    protected readonly IDbContext _context;
    protected DbSet<T> _dbSet;
    protected string _initialConnectionString;

    protected BaseRepository(IDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _initialConnectionString = _context.DB.GetConnectionString()!;
    }

    public virtual void SwitchConnectionString(string connectionString)
    {
        _context.DB.CloseConnection();
        _context.DB.SetConnectionString(connectionString);
        _dbSet = _context.Set<T>();
    }

    public virtual void ResetConnectionString()
    {
        _context.DB.CloseConnection();
        _context.DB.SetConnectionString(_initialConnectionString);
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
