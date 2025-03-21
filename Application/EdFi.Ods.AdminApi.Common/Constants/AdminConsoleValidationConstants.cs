// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdFi.Ods.AdminApi.Common.Constants;

public static class AdminConsoleValidationConstants
{
    public const string OdsIntanceIdIsNotValid = "The instance id is not valid.";
    public const string OdsIntanceIdStatusIsNotCompleted = "The instance cannot be deleted because it is not in a COMPLETED status.";
    public const string OdsInstanceIdStatusIsNotPendingDelete = "The instance status is invalid; it is not marked as 'Pending Delete'.";
}
