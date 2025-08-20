// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace EdFi.Ods.AdminApi.V1.Infrastructure
{
    [Serializable]
    [DebuggerDisplay("{DisplayName} - {Value}")]
    public abstract class Enumeration<TEnumeration> : Enumeration<TEnumeration, int>
            where TEnumeration : Enumeration<TEnumeration>
    {
        protected Enumeration(int value, string displayName)
            : base(value, displayName)
        {
        }

        public static TEnumeration? FromInt32(int value)
        {
            return FromValue(value);
        }

        public static bool TryFromInt32(int listItemValue, out TEnumeration? result)
        {
            return TryParse(listItemValue, out result);
        }
    }

    [Serializable]
    [DebuggerDisplay("{DisplayName} - {Value}")]
    public abstract class Enumeration<TEnumeration, TValue> : IComparable<TEnumeration>, IEquatable<TEnumeration>
        where TEnumeration : Enumeration<TEnumeration, TValue>
        where TValue : IComparable
    {
        private readonly string _displayName;
        private readonly TValue _value;

        private readonly static Lazy<TEnumeration[]> _enumerations = new(GetEnumerations);

        protected Enumeration(TValue value, string displayName)
        {
            _value = value;
            _displayName = displayName;
        }

        public TValue Value
        {
            get { return _value; }
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        public int CompareTo(TEnumeration? other)
        {
            return other is null ? 0 : Value.CompareTo(other.Value);
        }

        public override sealed string ToString()
        {
            return DisplayName;
        }

        public static TEnumeration[] GetAll()
        {
            return _enumerations.Value;
        }

        private static TEnumeration[] GetEnumerations()
        {
            Type enumerationType = typeof(TEnumeration);
            return enumerationType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(info => enumerationType.IsAssignableFrom(info.FieldType))
                .Select(info => info.GetValue(null))
                .Cast<TEnumeration>()
                .ToArray();
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as TEnumeration);
        }

        public bool Equals(TEnumeration? other)
        {
            return other is not null && Value.Equals(other.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(Enumeration<TEnumeration, TValue> left, Enumeration<TEnumeration, TValue> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Enumeration<TEnumeration, TValue> left, Enumeration<TEnumeration, TValue> right)
        {
            return !Equals(left, right);
        }

        public static TEnumeration? FromValue(TValue value)
        {
            return Parse(value, "value", item => item.Value.Equals(value));
        }

        public static TEnumeration? Parse(string displayName)
        {
            return Parse(displayName, "display name", item => item.DisplayName == displayName);
        }

        public static bool TryParse(Func<TEnumeration, bool> predicate, out TEnumeration? result)
        {
            result = GetAll().FirstOrDefault(predicate);
            return result is not null;
        }

        private static TEnumeration? Parse(object value, string description, Func<TEnumeration, bool> predicate)
        {
            if (!TryParse(predicate, out var result))
            {
                var message = string.Format("'{0}' is not a valid {1} in {2}", value, description, typeof(TEnumeration));
                throw new ArgumentException(message, "value");
            }

            return result;
        }

        public static bool TryParse(TValue value, out TEnumeration? result)
        {
            return TryParse(e => e.Value.Equals(value), out result);
        }

        public static bool TryParse(string displayName, out TEnumeration? result)
        {
            return TryParse(e => e.DisplayName == displayName, out result);
        }
    }
}
