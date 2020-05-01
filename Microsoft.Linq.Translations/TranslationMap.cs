// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Linq.Translations
{
    /// <summary>
    /// Maintains a list of mappings between properties and their compiled expressions.
    /// </summary>
    public class TranslationMap : Dictionary<MemberInfo, CompiledExpression>
    {
        internal static readonly TranslationMap DefaultMap = new TranslationMap();

        /// <summary>
        /// Get the <see cref="CompiledExpression{T, TResult}"/> for a given property.
        /// </summary>
        /// <typeparam name="T">Type of object this property is translated for.</typeparam>
        /// <typeparam name="TResult">Result type of property.</typeparam>
        /// <param name="method">Property definition to look-up.</param>
        /// <returns><see cref="CompiledExpression{T, TResult}"/> for this property.</returns>
        public CompiledExpression<T, TResult> Get<T, TResult>(MethodBase method)
        {
            Argument.EnsureNotNull("method", method);

            var propertyInfo = method.DeclaringType.GetRuntimeProperty(method.Name.Replace("get_", String.Empty));
            return this[propertyInfo] as CompiledExpression<T, TResult>;
        }

        /// <summary>
        /// Associate an existing <see cref="CompiledExpression{T, TResult}"/> to be translated for
        /// the specified <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="T">Type of object this property is translated for.</typeparam>
        /// <typeparam name="TResult">Result type of property.</typeparam>
        /// <param name="property">Property reference to associate this <see cref="CompiledExpression{T, TResult}"/> to.</param>
        /// <param name="compiledExpression">A <see cref="CompiledExpression{T, TResult}"/> to associate this <paramref name="property"/> with.</param>
        /// <returns><see cref="CompiledExpression{T, TResult}"/> for this property.</returns>
        public void Add<T, TResult>(Expression<Func<T, TResult>> property, CompiledExpression<T, TResult> compiledExpression)
        {
            Argument.EnsureNotNull("property", property);
            Argument.EnsureNotNull("compiledExpression", compiledExpression);

            Add(((MemberExpression)property.Body).Member, compiledExpression);
        }

        /// <summary>
        /// Associate a <paramref name="expression"/> to be translated for
        /// the specified <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="T">Type of object this property is translated for.</typeparam>
        /// <typeparam name="TResult">Result type of property.</typeparam>
        /// <param name="property">Property reference to associate this <paramref name="expression"/> to.</param>
        /// <param name="expression">Expression to associate with this <paramref name="property"/> with.</param>
        /// <returns><see cref="CompiledExpression{T, TResult}"/> for this property.</returns>
        public CompiledExpression<T, TResult> Add<T, TResult>(Expression<Func<T, TResult>> property, Expression<Func<T, TResult>> expression)
        {
            Argument.EnsureNotNull("property", property);
            Argument.EnsureNotNull("expression", expression);

            var compiledExpression = new CompiledExpression<T, TResult>(expression);
            Add(property, compiledExpression);
            return compiledExpression;
        }
    }
}