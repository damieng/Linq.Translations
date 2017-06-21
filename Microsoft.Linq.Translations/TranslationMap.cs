// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)
namespace Microsoft.Linq.Translations
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Maintains a list of mappings between properties and their compiled expressions.
    /// </summary>
    public class TranslationMap : Dictionary<MemberInfo, CompiledExpression>
    {
        internal static readonly TranslationMap DefaultMap = new TranslationMap();

        public CompiledExpression<T, TResult> Get<T, TResult>(MethodBase method)
        {
            Argument.EnsureNotNull("method", method);

            var propertyInfo = method.DeclaringType.GetRuntimeProperty(method.Name.Replace("get_", String.Empty));
            return this[propertyInfo] as CompiledExpression<T, TResult>;
        }

        public void Add<T, TResult>(Expression<Func<T, TResult>> property, CompiledExpression<T, TResult> compiledExpression)
        {
            Argument.EnsureNotNull("property", property);
            Argument.EnsureNotNull("compiledExpression", compiledExpression);

            Add(((MemberExpression)property.Body).Member, compiledExpression);
        }

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