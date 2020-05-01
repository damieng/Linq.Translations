// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Microsoft.Linq.Translations
{
    /// <summary>
    /// Extension methods over IQueryable to turn on expression translation via a
    /// specified or default TranslationMap.
    /// </summary>
    public static class ExpressiveExtensions
    {
        /// <summary>
        /// Create a new <see cref="IQueryable{T}"/> based upon the
        /// <paramref name="source"/> with the translatable properties decomposed back
        /// into their expression trees ready for translation to a remote provider using
        /// the default <see cref="TranslationMap"/>.
        /// </summary>
        /// <typeparam name="T">Result type of the query.</typeparam>
        /// <param name="source">Source query to translate.</param>
        /// <returns><see cref="IQueryable{T}"/> containing translated query.</returns>
        public static IQueryable<T> WithTranslations<T>(this IQueryable<T> source)
        {
            Argument.EnsureNotNull("source", source);

            return source.Provider.CreateQuery<T>(WithTranslations(source.Expression));
        }

        /// <summary>
        /// Create a new <see cref="IQueryable{T}"/> based upon the
        /// <paramref name="source"/> with the translatable properties decomposed back
        /// into their expression trees ready for translation to a remote provider using
        /// a specific <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="T">Result type of the query.</typeparam>
        /// <param name="source">Source query to translate.</param>
        /// <param name="map"><see cref="TranslationMap"/> used to translate property accesses.</param>
        /// <returns><see cref="IQueryable{T}"/> containing translated query.</returns>
        public static IQueryable<T> WithTranslations<T>(this IQueryable<T> source, TranslationMap map)
        {
            Argument.EnsureNotNull("source", source);
            Argument.EnsureNotNull("map", map);

            return source.Provider.CreateQuery<T>(WithTranslations(source.Expression, map));
        }

        /// <summary>
        /// Create a new <see cref="Expression"/> tree based upon the
        /// <paramref name="expression"/> with translatable properties decomposed back
        /// into their expression trees ready for translation to a remote provider using
        /// the default <see cref="TranslationMap"/>.
        /// </summary>
        /// <param name="expression">Expression tree to translate.</param>
        /// <returns><see cref="Expression"/> tree with translatable expressions translated.</returns>
        public static Expression WithTranslations(Expression expression)
        {
            Argument.EnsureNotNull("expression", expression);

            return WithTranslations(expression, TranslationMap.DefaultMap);
        }

        /// <summary>
        /// Create a new <see cref="Expression"/> tree based upon the
        /// <paramref name="expression"/> with translatable properties decomposed back
        /// into their expression trees ready for translation to a remote provider using
        /// the default <see cref="TranslationMap"/>.
        /// </summary>
        /// <param name="expression">Expression tree to translate.</param>
        /// <param name="map"><see cref="TranslationMap"/> used to translate property accesses.</param>
        /// <returns><see cref="Expression"/> tree with translatable expressions translated.</returns>
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

            protected override Expression VisitMember(MemberExpression node)
            {
                Argument.EnsureNotNull("node", node);

                EnsureTypeInitialized(node.Member.DeclaringType);

                if (map.TryGetValue(node.Member, out CompiledExpression cp))
                {
                    return VisitCompiledExpression(cp, node.Expression);
                }

                if (typeof(CompiledExpression).GetTypeInfo().IsAssignableFrom(node.Member.DeclaringType.GetTypeInfo()))
                {
                    return VisitCompiledExpression(cp, node.Expression);
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
