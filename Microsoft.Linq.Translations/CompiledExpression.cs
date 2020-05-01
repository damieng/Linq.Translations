// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)
using System;
using System.Linq.Expressions;

namespace Microsoft.Linq.Translations
{
    /// <summary>
    /// Provides the common boxed version of get.
    /// </summary>
    public abstract class CompiledExpression
    {
        internal abstract LambdaExpression BoxedGet { get; }
    }

    /// <summary>
    /// Represents an expression and its compiled function.
    /// </summary>
    /// <typeparam name="T">Class the expression relates to.</typeparam>
    /// <typeparam name="TResult">Return type of the expression.</typeparam>
    public sealed class CompiledExpression<T, TResult> : CompiledExpression
    {
        private readonly Expression<Func<T, TResult>> expression;
        private readonly Func<T, TResult> function;

        /// <summary>
        /// Creates a new instance of <see cref="CompiledExpression"/> for a given expression.
        /// </summary>
        /// <param name="expression">The expression to compile.</param>
        public CompiledExpression(Expression<Func<T, TResult>> expression)
        {
            this.expression = expression ?? throw new ArgumentNullException(nameof(expression));
            function = expression.Compile();
        }

        /// <summary>
        /// Evaluate a compiled expression against a specific instance of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="instance">Specific instance of <typeparamref name="T"/> to evaluate this
        /// compiled expresion against.</param>
        /// <returns><typeparamref name="TResult"/> result from evaluating this compiled expression against <paramref name="instance"/>.</returns>
        public TResult Evaluate(T instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            return function(instance);
        }

        internal override LambdaExpression BoxedGet
        {
            get { return expression; }
        }
    }
}