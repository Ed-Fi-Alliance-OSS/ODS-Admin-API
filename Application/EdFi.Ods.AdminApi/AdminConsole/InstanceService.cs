// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Dynamic;
using AutoMapper;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Queries;
using EdFi.Ods.AdminApi.Features.ODSInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using log4net;
using Newtonsoft.Json;
using static EdFi.Ods.AdminApi.AdminConsole.Features.Instances.AddInstance;

namespace EdFi.Ods.AdminApi.AdminConsole;

public interface IAdminConsoleInstancesService
{
    Task InitializeInstancesAsync(int tenantId, int applicationId);
}
public class InstanceService : IAdminConsoleInstancesService
{
    private readonly IGetOdsInstancesQuery _getOdsInstancesQuery;
    private readonly IGetOdsInstanceContextsQuery _getOdsInstanceContextsQuery;
    private readonly IGetOdsInstanceDerivativesQuery _getOdsInstanceDerivativesQuery;
    private readonly IAddInstanceCommand _addInstanceCommand;
    private readonly IGetInstancesQuery _getInstancesQuery;
    private readonly IGetApiClientIdByApplicationIdQuery _getApiClientIdByApplicationIdQuery;
    private readonly IMapper _mapper;

    private static readonly ILog _log = LogManager.GetLogger(typeof(InstanceService));


    public InstanceService(IGetOdsInstancesQuery getOdsInstancesQuery,
        IGetOdsInstanceContextsQuery getOdsInstanceContextsQuery,
        IGetOdsInstanceDerivativesQuery getOdsInstanceDerivativesQuery,
        IAddInstanceCommand addInstanceCommand,
        IGetInstancesQuery getInstancesQuery,
        IGetApiClientIdByApplicationIdQuery getApiClientIdByApplicationIdQuery,
        IMapper mapper)
    {
        _getOdsInstancesQuery = getOdsInstancesQuery;
        _getOdsInstanceContextsQuery = getOdsInstanceContextsQuery;
        _getOdsInstanceDerivativesQuery = getOdsInstanceDerivativesQuery;
        _addInstanceCommand = addInstanceCommand;
        _getInstancesQuery = getInstancesQuery;
        _getApiClientIdByApplicationIdQuery = getApiClientIdByApplicationIdQuery;
        _mapper = mapper;
    }

    public async Task InitializeInstancesAsync(int tenantId, int applicationId)
    {
        //get instances in adminconsole
        var instancesAdminConsole = await _getInstancesQuery.Execute(null);

        //get odsinstances

        var odsInstancesList = _getOdsInstancesQuery.Execute();
        var odsInstances = _mapper.Map<List<OdsInstanceModel>>(odsInstancesList);
        var odsInstanceContexts = _getOdsInstanceContextsQuery.Execute();
        var odsInstanceDerivatives = _getOdsInstanceDerivativesQuery.Execute();
        foreach (var odsInstance in odsInstances)
        {
            //check if exist
            if (!instancesAdminConsole.Any(x => x.OdsInstanceId == odsInstance.OdsInstanceId))
            {
                //create
                AddInstanceRequest addInstanceRequest = new AddInstanceRequest();
                addInstanceRequest.OdsInstanceId = odsInstance.OdsInstanceId;
                addInstanceRequest.TenantId = tenantId;
                addInstanceRequest.InstanceType = odsInstance.InstanceType;
                addInstanceRequest.Name = odsInstance.Name;
                addInstanceRequest.Status = nameof(InstanceStatus.Completed);

                var apiClient = _mapper.Map<ApiClient>(_getApiClientIdByApplicationIdQuery.Execute(applicationId));

                dynamic apiCredentials = new ExpandoObject();
                apiCredentials.ClientId = apiClient.Key;
                apiCredentials.Secret = apiClient.Secret;
                addInstanceRequest.Credentials = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(apiCredentials));

                var odsContexts = _mapper.Map<List<Infrastructure.Services.Instances.Models.OdsInstanceContextModel>>(odsInstanceContexts.Where(x => x.OdsInstance.OdsInstanceId == odsInstance.OdsInstanceId));
                var odsDerivatives = _mapper.Map<List<Infrastructure.Services.Instances.Models.OdsInstanceDerivativeModel>>(odsInstanceDerivatives.Where(x => x.OdsInstance.OdsInstanceId == odsInstance.OdsInstanceId));
                addInstanceRequest.OdsInstanceContexts = odsContexts;
                addInstanceRequest.OdsInstanceDerivatives = odsDerivatives;
                await _addInstanceCommand.Execute(addInstanceRequest);
            }
        }
        _log.Info("Instances have been created in the AdminConsole tables");
    }
}


