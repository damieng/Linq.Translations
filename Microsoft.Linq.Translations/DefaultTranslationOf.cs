// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Linq.Translations
{
    /// <summary>
    /// Simple fluent way to access the default translation map.
    /// </summary>
    /// <typeparam name="T">Class the expression uses.</typeparam>
    public static class DefaultTranslationOf<T>
    {
        /// <summary>
        /// Defined a property and its associated expression and register it with the 
        /// default <see cref="TranslationMap"/>.
        /// </summary>
        /// <typeparam name="TResult">Type this property returns.</typeparam>
        /// <param name="property">Reference to the property to translate.</param>
        /// <param name="expression">Expression that this property should be translated to.</param>
        /// <returns>A <see cref="CompiledExpression{T, TResult}"/> with details of this property including a compiled version for local evaluation.</returns>
        public static CompiledExpression<T, TResult> Property<TResult>(Expression<Func<T, TResult>> property, Expression<Func<T, TResult>> expression)
        {
            return TranslationMap.DefaultMap.Add(property, expression);
        }

        /// <summary>
        /// Start defining a property so that you can fluently chain the definition of this property
        /// using the <see cref="IncompletePropertyTranslation{TResult}.Is(Expression{Func{T, TResult}})"/> method.
        /// </summary>
        /// <typeparam name="TResult">Type this property returns.</typeparam>
        /// <param name="property">Reference to the property to translate.</param>
        /// <returns><see cref="IncompletePropertyTranslation{TResult}"/> placeholder to allow the fluent syntax to continue.</returns>
        public static IncompletePropertyTranslation<TResult> Property<TResult>(Expression<Func<T, TResult>> property)
        {
            return new IncompletePropertyTranslation<TResult>(property);
        }

        /// <summary>
        /// Evaluate a property for a given instance using the default <see cref="TranslationMap"/>/
        /// </summary>
        /// <typeparam name="TResult">Type this property returns.</typeparam>
        /// <param name="instance">Instance of the <typeparamref name="T"/> to evaluate against.</param>
        /// <param name="method"><see cref="MethodBase"/> of the property getter to be evaluated.</param>
        /// <returns><typeparamref name="TResult"/> containing the result of evaluation.</returns>
        public static TResult Evaluate<TResult>(T instance, MethodBase method)
        {
            var compiledExpression = TranslationMap.DefaultMap.Get<T, TResult>(method);
            return compiledExpression.Evaluate(instance);
        }

        /// <summary>
        /// Partially defined property translation used to fluently construct a full
        /// property translation.
        /// </summary>
        /// <remarks>
        /// You should not ever need to directly use this class. It is purely to facilitate
        /// the building of fluent property translation syntax.
        /// </remarks>
        /// <typeparam name="TResult">Result type of the property translation.</typeparam>
#pragma warning disable CA1034 // Nested types should not be visible - used for fluent chaining only.
        public class IncompletePropertyTranslation<TResult>
#pragma warning restore CA1034 // Nested types should not be visible
        {
            private readonly Expression<Func<T, TResult>> property;

            /// <summary>
            /// Create a new instance of <see cref="IncompletePropertyTranslation{TResult}"/>.
            /// </summary>
            /// <param name="property">An <see cref="Expression"/> that defines the property.</param>
            internal IncompletePropertyTranslation(Expression<Func<T, TResult>> property)
            {
                this.property = property;
            }

            /// <summary>
            /// Complete a translatable property using the fluent syntax by specifying the
            /// expression this property should be translated to.
            /// </summary>
            /// <param name="expression">Expression this property should be translated to.</param>
            /// <returns><see cref="CompiledExpression{T, TResult}"/> containing the compiled expressoin for translation.</returns>
            public CompiledExpression<T, TResult> Is(Expression<Func<T, TResult>> expression)
            {
                return Property(property, expression);
            }
        }
    }
}