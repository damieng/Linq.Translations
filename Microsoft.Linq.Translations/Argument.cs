// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace Microsoft.Linq.Translations
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    /// <summary>
    /// Argument validation static helpers to reduce noise in other methods.
    /// </summary>
    [DebuggerStepThrough]
    public static class Argument
    {
        public static void EnsureNotNull(string parameterName, object value)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
        }

        public static void EnsurePositive(string parameterName, TimeSpan value)
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(parameterName, "Must be a positive TimeSpan.");
        }

        public static void EnsureNotBlank(string parameterName, string value)
        {
            EnsureNotNull(parameterName, value);
            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Cannot be a blank string.", parameterName);
        }

        public static void EnsureIsAssignableFrom<T>(string parameterName, Type type)
        {
            if (!typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                throw new ArgumentException(string.Format("Type {0} must be assignable from {1}", type.Name, typeof(T).Name), parameterName);
        }

        public static void EnsureIsDefinedEnum<T>(string parameterName, T value) where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentOutOfRangeException(parameterName, string.Format("Must be a defined {0} enum value.", typeof(T)));
        }

        public static void EnsureNotEmpty<T>(string parameterName, T[] values)
        {
            if (values == null || values.Length < 1)
                throw new ArgumentOutOfRangeException(parameterName, "Must contain one or more values");
        }
    }
}