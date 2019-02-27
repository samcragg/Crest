// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Util
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq.Expressions;

    /// <content>
    /// Contains the nested <see cref="DelegateBuilder"/> class.
    /// </content>
    internal partial class LinkExpressionBuilder
    {
        private class DelegateBuilder
        {
            private readonly ParameterExpression argumentArray =
                Expression.Parameter(typeof(object[]));

            private readonly ParameterExpression buffer =
                Expression.Parameter(typeof(StringBuffer));

            private readonly List<Expression> expressions =
                new List<Expression>();

            private readonly ParameterExpression queryAdded =
                Expression.Parameter(typeof(bool));

            internal DelegateBuilder(int version)
            {
                this.expressions.Add(
                    Expression.Assign(this.buffer, Expression.New(typeof(StringBuffer))));

                this.AddLiteral("/v" + version.ToString(NumberFormatInfo.InvariantInfo));
            }

            internal void AddCapture(Type type, int index)
            {
                this.expressions.Add(this.AppendArgumentExpression(type, index));
            }

            internal void AddLiteral(string value)
            {
                this.expressions.Add(this.AppendLiteralExpression(value));
            }

            internal void AddQuery(string key, Type type, int index)
            {
                Expression getArgument = Expression.ArrayIndex(
                    this.argumentArray,
                    Expression.Constant(index));

                Expression writeValue;
                if (key == null)
                {
                    writeValue = Expression.Block(
                        this.AddQuerySeparatorExpression(),
                        Expression.Call(AppendDynamicQueryMethod, this.buffer, getArgument));
                }
                else
                {
                    writeValue = Expression.Block(
                        this.AddQuerySeparatorExpression(),
                        this.AppendLiteralExpression(key + "="),
                        this.AppendArgumentExpression(type, index));
                }

                this.expressions.Add(
                    Expression.IfThen(
                        Expression.ReferenceNotEqual(
                            getArgument,
                            Expression.Constant(null, typeof(object))),
                        writeValue));
            }

            internal Expression<T> Build<T>()
            {
                this.expressions.Add(
                    Expression.Call(this.buffer, StringBufferToString));

                Expression convertToString =
                    Expression.TryFinally(
                        Expression.Block(this.expressions),
                        Expression.Call(this.buffer, StringBufferDispose));

                return Expression.Lambda<T>(
                    Expression.Block(
                        new[] { this.buffer, this.queryAdded },
                        new[] { convertToString }),
                    this.argumentArray);
            }

            private Expression AddQuerySeparatorExpression()
            {
                // If we've not added any query values then we need to add the
                // query separator (?), otherwise, we need to add the '&' to
                // separate values within the query
                return Expression.IfThenElse(
                    Expression.IsFalse(this.queryAdded),
                    Expression.Block(
                        Expression.Assign(this.queryAdded, Expression.Constant(true)),
                        Expression.Call(
                            this.buffer,
                            StringBufferAppendChar,
                            Expression.Constant('?'))),
                    Expression.Call(
                        this.buffer,
                        StringBufferAppendChar,
                        Expression.Constant('&')));
            }

            private Expression AppendArgumentExpression(Type type, int index)
            {
                return UrlValueConverter.AppendValue(
                    this.buffer,
                    this.argumentArray,
                    index,
                    type);
            }

            private Expression AppendLiteralExpression(string value)
            {
                return Expression.Call(
                    this.buffer,
                    StringBufferAppend,
                    Expression.Constant(value));
            }
        }
    }
}
