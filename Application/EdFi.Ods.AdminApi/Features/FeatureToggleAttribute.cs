// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.Features
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class FeatureToggleAttribute : ActionFilterAttribute, IFeatureToggleAttribute
    {
        private readonly string _featureName;
        private readonly IConfiguration _configuration;

        public FeatureToggleAttribute(string featureName, IConfiguration configuration)
        {
            _featureName = featureName;
            _configuration = configuration;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var isDisabled = !_configuration.GetValue<bool>(_featureName);
            if (isDisabled)
            {
                context.Result = new NotFoundResult();
            }
            base.OnActionExecuting(context);
        }
    }
}

