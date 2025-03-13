// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore.Storage;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;

public interface ICommandRepository<T> : IBaseRepository where T : class
{
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(T entity);

    IDbContextTransaction BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();
    void DisposeTransaction();
}

public class CommandRepository<T>(IDbContext context) : BaseRepository<T>(context), ICommandRepository<T> where T : class
{
    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await SaveChangesAsync();
        return entity;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public IDbContextTransaction BeginTransaction()
    {
        return _context.BeginTransaction();
    }

    public void CommitTransaction()
    {
        _context.CommitTransaction();
    }

    public void RollbackTransaction()
    {
        _context.RollbackTransaction();
    }

    public void DisposeTransaction()
    {
        _context.DisposeTransaction();
    }
}
