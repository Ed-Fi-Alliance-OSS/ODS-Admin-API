// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetEducationOrganizationQuery
{
    List<EducationOrganization> Execute();

    List<EducationOrganization> Execute(int odsInstanceId);

    List<EducationOrganization> Execute(string databaseEngine, string adminConnectionString, int odsInstanceId);
}

public class GetEducationOrganizationQuery(AdminApiDbContext adminApiDbContext) : IGetEducationOrganizationQuery
{
    private readonly AdminApiDbContext _adminApiDbContext = adminApiDbContext;

    public List<EducationOrganization> Execute()
    {
        return _adminApiDbContext.EducationOrganizations.ToList();
    }


    public List<EducationOrganization> Execute(int odsInstanceId)
    {
        return _adminApiDbContext.EducationOrganizations
                .Where(edOrgs => edOrgs.InstanceId == odsInstanceId)
                .ToList();
    }

    public List<EducationOrganization> Execute(string databaseEngine, string adminConnectionString, int odsInstanceId)
    {

        if (databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddInMemoryCollection(
                    new Dictionary<string, string?>
                   {
                        { "AppSettings:DatabaseEngine", "SqlServer" }
                   })
               .Build();

            var adminApiOptionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
            adminApiOptionsBuilder.UseSqlServer(adminConnectionString);

            using (AdminApiDbContext temporaryAdminApiDbContext = new(adminApiOptionsBuilder.Options, configuration))
            {
                return temporaryAdminApiDbContext.EducationOrganizations
                    .Where(edOrgs => edOrgs.InstanceId == odsInstanceId)
                    .ToList();
            }
        }
        else if (databaseEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddInMemoryCollection(
                    new Dictionary<string, string?>
                   {
                        { "AppSettings:DatabaseEngine", "PostgreSql" }
                   })
               .Build();

            var adminApiOptionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
            adminApiOptionsBuilder.UseNpgsql(adminConnectionString);
            adminApiOptionsBuilder.UseLowerCaseNamingConvention();

            using (AdminApiDbContext temporaryAdminApiDbContext = new(adminApiOptionsBuilder.Options, configuration))
            {
                return temporaryAdminApiDbContext.EducationOrganizations
                    .Where(edOrgs => edOrgs.InstanceId == odsInstanceId)
                    .ToList();
            }
        }
        else
        {
            throw new NotSupportedException($"Database engine '{databaseEngine}' is not supported.");
        }
    }
}
