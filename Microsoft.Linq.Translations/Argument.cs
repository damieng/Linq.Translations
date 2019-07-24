// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace Microsoft.Linq.Translations
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Argument validation static helpers to reduce noise in other methods.
    /// </summary>
    [DebuggerStepThrough]
    internal static class Argument
    {
        public static void EnsureNotNull(string parameterName, object value)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
        }
    }
}