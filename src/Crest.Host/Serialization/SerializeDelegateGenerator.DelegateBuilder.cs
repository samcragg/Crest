// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Crest.Host.Serialization.Internal;

    /// <content>
    /// Contains the nested helper <see cref="DelegateBuilder"/> class.
    /// </content>
    internal partial class SerializeDelegateGenerator
    {
        private class DelegateBuilder
        {
            private readonly List<Expression> expressions = new List<Expression>();

            public DelegateBuilder(Type type, MetadataBuilder metadataBuilder)
            {
                this.MetadataBuilder = metadataBuilder;
                this.TypedInstance = Expression.Variable(type, "typedInstance");
                this.expressions.Add(
                    Expression.Assign(
                        this.TypedInstance,
                        Expression.Convert(this.RawInstance, type)));
            }

            public ParameterExpression Formatter { get; }
                = Expression.Parameter(typeof(IFormatter), "formatter");

            public ParameterExpression Metadata { get; }
                = Expression.Parameter(typeof(IReadOnlyList<object>), "metadata");

            public MetadataBuilder MetadataBuilder { get; }

            public ParameterExpression RawInstance { get; }
                = Expression.Parameter(typeof(object), "instance");

            public ParameterExpression TypedInstance { get; }

            public void Add(Expression expression)
            {
                this.expressions.Add(expression);
            }

            public Expression BuildBody()
            {
                return Expression.Block(
                    new[] { this.TypedInstance },
                    this.expressions);
            }
        }
    }
}
