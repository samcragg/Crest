// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Inspects parameter usage in an expression.
    /// </summary>
    internal sealed class ParameterVisitor : ExpressionVisitor
    {
        private readonly HashSet<ParameterExpression> parameters = new HashSet<ParameterExpression>();

        /// <summary>
        /// Finds a parameter of the specified type, ensuing there is only a
        /// single parameter usage in the expression.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="parameterType">The type of the parameter to find.</param>
        /// <returns>
        /// The parameter of the specified type if found; otherwise, null.
        /// </returns>
        internal ParameterExpression FindParameter(Expression expression, Type parameterType)
        {
            this.parameters.Clear();
            this.Visit(expression);

            if (this.parameters.Count == 1)
            {
                ParameterExpression parameter = this.parameters.First();
                if (parameter.Type == parameterType)
                {
                    return parameter;
                }
            }

            return null;
        }

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            this.parameters.Add(node);
            return base.VisitParameter(node);
        }
    }
}
