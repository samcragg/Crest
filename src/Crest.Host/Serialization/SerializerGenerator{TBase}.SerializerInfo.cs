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
                    serializer.GetConstructor(new[] { typeof(Stream) });

                this.SerializeArrayMethod = CreateSerializeArrayMethod(constructor);
                this.SerializeObjectMethod = CreateSerializeObjectMethod(constructor);
                this.SerializerType = serializer;
            }

            private static Action<Stream, object> CreateSerializeArrayMethod(ConstructorInfo constructor)
            {
                MethodInfo writeMethod =
                    typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.WriteArray));

                ParameterExpression stream = Expression.Parameter(typeof(Stream));
                ParameterExpression value = Expression.Parameter(typeof(object));
                var lambda = Expression.Lambda<Action<Stream, object>>(
                    Expression.Call(
                        Expression.New(constructor, stream),
                        writeMethod,
                        Expression.Convert(value, typeof(Array))),
                    stream,
                    value);
                return lambda.Compile();
            }

            private static Action<Stream, object> CreateSerializeObjectMethod(ConstructorInfo constructor)
            {
                MethodInfo writeMethod =
                    typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.Write));

                ParameterExpression stream = Expression.Parameter(typeof(Stream));
                ParameterExpression value = Expression.Parameter(typeof(object));
                var lambda = Expression.Lambda<Action<Stream, object>>(
                    Expression.Call(
                        Expression.New(constructor, stream),
                        writeMethod,
                        value),
                    stream,
                    value);
                return lambda.Compile();
            }
        }
    }
}
