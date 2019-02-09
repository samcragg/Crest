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
    internal partial class DeserializeDelegateGenerator
    {
        private class DelegateBuilder
        {
            private readonly List<Expression> expressions = new List<Expression>();

            public DelegateBuilder(Type type, MetadataBuilder metadataBuilder)
            {
                this.Instance = Expression.Variable(type, "instance");
                this.MetadataBuilder = metadataBuilder;
            }

            public ParameterExpression Formatter { get; }
                = Expression.Parameter(typeof(IFormatter), "formatter");

            public Expression InitializeInstance { get; set; }

            public ParameterExpression Instance { get; }

            public ParameterExpression Metadata { get; }
                = Expression.Parameter(typeof(IReadOnlyList<object>), "metadata");

            public MetadataBuilder MetadataBuilder { get; }

            public void Add(Expression expression)
            {
                this.expressions.Add(expression);
            }

            public Expression BuildBody(Methods methods)
            {
                if (this.InitializeInstance == null)
                {
                    Expression assignInstance = Expression.Assign(
                        this.Instance,
                        Expression.New(this.Instance.Type));

                    return Expression.Condition(
                        Expression.Call(
                            Expression.Property(this.Formatter, methods.ClassReader.GetReader),
                            methods.ValueReader.ReadNull),
                        Expression.Constant(null, typeof(object)),
                        this.BuildReadBlock(assignInstance));
                }
                else
                {
                    return this.BuildReadBlock(
                        Expression.Assign(this.Instance, this.InitializeInstance));
                }
            }

            private Expression BuildReadBlock(Expression assignInstance)
            {
                this.expressions.Insert(0, assignInstance);
                this.expressions.Add(Expression.Convert(this.Instance, typeof(object)));

                return Expression.Block(
                        new[] { this.Instance },
                        this.expressions);
            }
        }
    }
}
