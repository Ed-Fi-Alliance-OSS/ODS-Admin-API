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

    public class AsyncLocalContextStorage : IContextStorage
    {
        private static readonly AsyncLocal<Dictionary<string, object?>> _storage = new();

        private static Dictionary<string, object?> Current =>
            _storage.Value ??= new Dictionary<string, object?>();

        public void SetValue(string key, object value) => Current[key] = value;

        public T? GetValue<T>(string key) => Current.TryGetValue(key, out var v) ? (T?)v : default;
    }
}
