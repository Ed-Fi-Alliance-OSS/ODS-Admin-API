// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Context
{
    public interface IContextStorage
    {
        void SetValue(string key, object value);

        T? GetValue<T>(string key);
    }

    public class HashtableContextStorage : IContextStorage
    {

        public Hashtable UnderlyingHashtable { get; } = [];

        public void SetValue(string key, object value)
        {
            UnderlyingHashtable[key] = value;
        }

        public T? GetValue<T>(string key) => (T?)(UnderlyingHashtable != null &&
            UnderlyingHashtable[key] != null ? UnderlyingHashtable[key] : default(T));
    }
}
