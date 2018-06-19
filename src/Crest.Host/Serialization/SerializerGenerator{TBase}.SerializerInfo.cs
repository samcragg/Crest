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
    using Crest.Host.Serialization.Internal;

    /// <content>
    /// Contains the nested <see cref="SerializerInfo"/> struct.
    /// </content>
    internal partial class SerializerGenerator<TBase>
    {
        private struct SerializerInfo
        {
            public readonly Func<Stream, object> DeserializeArrayMethod;

            public readonly Func<Stream, object> DeserializeObjectMethod;

            public readonly Action<Stream, object> SerializeArrayMethod;

            public readonly Action<Stream, object> SerializeObjectMethod;

            public readonly Type SerializerType;

            public SerializerInfo(Type serializer)
            {
                ConstructorInfo constructor =
                    serializer.GetConstructor(new[] { typeof(Stream), typeof(SerializationMode) });

                this.DeserializeArrayMethod = CreateDeserializeMethodCall(
                    constructor,
                    typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.ReadArray)));

                this.DeserializeObjectMethod = CreateDeserializeMethodCall(
                    constructor,
                    typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.Read)));

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

            private static Func<Stream, object> CreateDeserializeMethodCall(
                ConstructorInfo constructor,
                MethodInfo method)
            {
                ParameterExpression stream = Expression.Parameter(typeof(Stream));
                ConstantExpression deserialize = Expression.Constant(SerializationMode.Deserialize);

                var lambda = Expression.Lambda<Func<Stream, object>>(
                    Expression.Call(
                        Expression.New(constructor, stream, deserialize),
                        method),
                    stream);

                return lambda.Compile();
            }

            private static Action<Stream, object> CreateSerializeMethodCall(
                ConstructorInfo constructor,
                MethodInfo method,
                Func<ParameterExpression, Expression> loadValue)
            {
                MethodInfo flushMethod = typeof(ITypeSerializer).GetMethod(nameof(ITypeSerializer.Flush));
                ParameterExpression instance = Expression.Variable(constructor.DeclaringType);
                ParameterExpression stream = Expression.Parameter(typeof(Stream));
                ConstantExpression serialize = Expression.Constant(SerializationMode.Serialize);
                ParameterExpression value = Expression.Parameter(typeof(object));

                // var x = new Serializer()
                // x.method(loadValue)
                // x.Flush()
                Expression body = Expression.Block(
                    new[] { instance },
                    Expression.Assign(instance, Expression.New(constructor, stream, serialize)),
                    Expression.Call(instance, method, loadValue(value)),
                    Expression.Call(instance, flushMethod));

                return Expression.Lambda<Action<Stream, object>>(body, stream, value)
                       .Compile();
            }
        }
    }
}
