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

            var propertyInfo = method.DeclaringType.GetProperty(method.Name.Replace("get_", String.Empty));
            return this[propertyInfo] as CompiledExpression<T, TResult>;
        }

        public void Add<T, TResult>(Expression<Func<T, TResult>> property, CompiledExpression<T, TResult> compiledExpression)
        {
            Argument.EnsureNotNull("property", property);
            Argument.EnsureNotNull("compiledExpression", compiledExpression);
            
            // deltafsdevelopment May 2013 - Fix to the code here so that if the prop is an override the derived class type name is
            // is stored in the translation map not the base class name that way we can have different expressions mapped to the
            // same override in different derived classes
            if (property.Parameters.Count > 0)
            {
              ParameterExpression expr = property.Parameters[0];
              PropertyInfo pi = expr.Type.GetProperty(((MemberExpression)property.Body).Member.Name);
              base.Add(pi, compiledExpression);
            }
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