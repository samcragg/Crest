// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <content>
    /// Contains the nested <see cref="SerializerInfo"/> struct.
    /// </content>
    internal partial class SerializerGenerator<TBase>
    {
        private struct SerializerInfo
        {
            public readonly Action<Stream, object> SerializeArrayMethod;

            public readonly Action<Stream, object> SerializeObjectMethod;

            public readonly Type SerializerType;

            public SerializerInfo(Type serializer)
            {
                ConstructorInfo constructor =
                    serializer.GetConstructor(new[] { typeof(Stream), typeof(SerializationMode) });

                this.SerializeArrayMethod = CreateSerializeMethodCall(
                    constructor,
                    typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.WriteArray)),
                    value => Expression.Convert(value, typeof(Array)));

                this.SerializeObjectMethod = CreateSerializeMethodCall(
                    constructor,
                    typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.Write)),
                    value => value);

                this.SerializerType = serializer;
            }

            private static Action<Stream, object> CreateSerializeMethodCall(
                ConstructorInfo constructor,
                MethodInfo method,
                Func<ParameterExpression, Expression> loadValue)
            {
                ParameterExpression stream = Expression.Parameter(typeof(Stream));
                ConstantExpression serialize = Expression.Constant(SerializationMode.Serialize);
                ParameterExpression value = Expression.Parameter(typeof(object));

                var lambda = Expression.Lambda<Action<Stream, object>>(
                    Expression.Call(
                        Expression.New(constructor, stream, serialize),
                        method,
                        loadValue(value)),
                    stream,
                    value);

                return lambda.Compile();
            }
        }
    }
}
