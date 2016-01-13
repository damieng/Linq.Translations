// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)
namespace Microsoft.Linq.Translations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.CompilerServices;
    using System.Reflection;
    /// <summary>
    /// Extension methods over IQueryable to turn on expression translation via a
    /// specified or default TranslationMap.
    /// </summary>
    public static class ExpressiveExtensions
    {
        public static IQueryable<T> WithTranslations<T>(this IQueryable<T> source)
        {
            Argument.EnsureNotNull("source", source);

            return source.Provider.CreateQuery<T>(WithTranslations(source.Expression));
        }

        public static IQueryable<T> WithTranslations<T>(this IQueryable<T> source, TranslationMap map)
        {
            Argument.EnsureNotNull("source", source);
            Argument.EnsureNotNull("map", map);

            return source.Provider.CreateQuery<T>(WithTranslations(source.Expression, map));
        }

        public static Expression WithTranslations(Expression expression)
        {
            Argument.EnsureNotNull("expression", expression);

            return WithTranslations(expression, TranslationMap.DefaultMap);
        }

        public static Expression WithTranslations(Expression expression, TranslationMap map)
        {
            Argument.EnsureNotNull("expression", expression);
            Argument.EnsureNotNull("map", map);

            return new TranslatingVisitor(map).Visit(expression);
        }

        private static void EnsureTypeInitialized(Type type)
        {
            Argument.EnsureNotNull("type", type);

            try
            {
                // Ensure the static members are accessed class' ctor
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
            catch (TypeInitializationException)
            {
            }
        }

        /// <summary>
        /// Extends the expression visitor to translate properties to expressions
        /// according to the provided translation map.
        /// </summary>
        private class TranslatingVisitor : ExpressionVisitor
        {
            private readonly Stack<KeyValuePair<ParameterExpression, Expression>> bindings = new Stack<KeyValuePair<ParameterExpression, Expression>>();
            private readonly TranslationMap map;

            internal TranslatingVisitor(TranslationMap map)
            {
                Argument.EnsureNotNull("map", map);

                this.map = map;
            }

            /// <summary>
            ///  Walk up the inheritance heirarchy searching for a compiled expression attached to a
            ///  property of the given name
            /// </summary>
            /// <param name="propName">Name of the property to search for</param>
            /// <param name="type">Type of the member to search against</param>
            /// <returns>Compiled expression if found or null if not</returns>
            private CompiledExpression FindCompiledExpression(String propName, Type type)
            {
                while (type != typeof(Object))
                {
                    CompiledExpression cp;
                    MemberInfo mi = type.GetProperty(propName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    EnsureTypeInitialized(type);
                    if (mi != null && map.TryGetValue(mi, out cp))
                    {
                        return cp;
                    }
                    type = type.BaseType;
                }
                return null;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                Argument.EnsureNotNull("node", node);
                // deltafsdevelopment 10 Nov 2015 - Fix to original code here so that the code searches for CompiledExpressions
                // right through the inheritance heirarchy to allow them to be defined on base classes and on overrides
                if (node.Expression != null)
                {
                    Type type = node.Expression.Type;
                    String propName = node.Member.Name;
                    CompiledExpression cp = FindCompiledExpression(propName, type);
                    if (cp != null)
                    {
                        return VisitCompiledExpression(cp, node.Expression);
                    }
                }
                return base.VisitMember(node);

            }

            private Expression VisitCompiledExpression(CompiledExpression ce, Expression expression)
            {
                bindings.Push(new KeyValuePair<ParameterExpression, Expression>(ce.BoxedGet.Parameters.Single(), expression));
                var body = Visit(ce.BoxedGet.Body);
                bindings.Pop();
                return body;
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                var binding = bindings.FirstOrDefault(b => b.Key == p);
                return (binding.Value == null) ? base.VisitParameter(p) : Visit(binding.Value);
            }
        }
    }
}
