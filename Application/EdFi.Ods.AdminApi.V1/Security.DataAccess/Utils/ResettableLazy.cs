// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading;

namespace EdFi.Ods.AdminApi.V1.Security.DataAccess.Utils
{
    public class ResettableLazy<T>
    {
        private readonly Func<T> _valueFactory;
        private Lazy<T> _lazy;

        public bool IsValueCreated => _lazy.IsValueCreated;
        public T Value => _lazy.Value;

        public ResettableLazy(Func<T> valueFactory)
        {
            _valueFactory = valueFactory;
            _lazy = new Lazy<T>(_valueFactory, LazyThreadSafetyMode.PublicationOnly);
        }

        public void Reset()
        {
            _lazy = new Lazy<T>(_valueFactory, LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
