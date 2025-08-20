// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Runtime.CompilerServices;
using log4net;

namespace EdFi.Ods.AdminApi.V1.Infrastructure;

public interface IOdsSecurityModelVersionResolver2
{
    EdFiOdsSecurityModelCompatibility2 DetermineSecurityModel();
}

public class OdsSecurityVersionResolver2 : IOdsSecurityModelVersionResolver2
{
    private readonly ILog _log;
    private readonly Lazy<EdFiOdsSecurityModelCompatibility2> _modelVersion;
    private readonly string _odsApiVersion;

    public OdsSecurityVersionResolver2(string odsApiVersion)
    {
        _odsApiVersion = odsApiVersion;
        _log = LogManager.GetLogger(typeof(OdsSecurityVersionResolver2));
        _modelVersion = new Lazy<EdFiOdsSecurityModelCompatibility2>(InitializeModelVersion);
    }

    public EdFiOdsSecurityModelCompatibility2 DetermineSecurityModel() => _modelVersion.Value;

    private EdFiOdsSecurityModelCompatibility2 InitializeModelVersion()
    {
        try
        {
            return _odsApiVersion switch
            {
                "5.3" => EdFiOdsSecurityModelCompatibility2.ThreeThroughFive,
                "5.3-cqe" => EdFiOdsSecurityModelCompatibility2.FiveThreeCqe,
                "6.0" or "6.1" => EdFiOdsSecurityModelCompatibility2.Six,
                _ => throw new SwitchExpressionException()
            };
        }
        catch (SwitchExpressionException)
        {
            _log.Error("OdsApiVersion not configured. Valid values are 5.3, 5.3-cqe, 6.0 and 6.1");
            throw new Exception("OdsApiVersion not configured. Valid values are 5.3, 5.3-cqe, 6.0 and 6.1");
        }
    }
}

public class EdFiOdsSecurityModelCompatibilityException2 : NotImplementedException
{
    public EdFiOdsSecurityModelCompatibilityException2()
        : base("Handling for security model not implemented") { }
    public EdFiOdsSecurityModelCompatibilityException2(EdFiOdsSecurityModelCompatibility2 version)
        : base($"Handling for security model for version {version} not implemented") { }
    public EdFiOdsSecurityModelCompatibilityException2(string message) : base(message) { }
}

public enum EdFiOdsSecurityModelCompatibility2
{
    ThreeThroughFive = 1,
    Six = 2,
    Both = 3,
    FiveThreeCqe = 4
}
