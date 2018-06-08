// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Generates serializers at runtime for specific types.
    /// </summary>
    /// <typeparam name="TBase">
    /// The type of the base class the generated serializers will inherit.
    /// </typeparam>
    internal sealed partial class SerializerGenerator<TBase> : SerializerGenerator, ISerializerGenerator<TBase>
    {
        private readonly ClassSerializerGenerator classSerializer;
        private readonly EnumSerializerGenerator enumSerializer;

        private readonly Dictionary<Type, SerializerInfo> knownTypes =
                    new Dictionary<Type, SerializerInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerGenerator{TBase}"/> class.
        /// </summary>
        public SerializerGenerator()
        {
            this.classSerializer = new ClassSerializerGenerator(
                this.GetSerializerFor,
                ModuleBuilder,
                typeof(TBase));

            this.enumSerializer = new EnumSerializerGenerator(
                ModuleBuilder,
                typeof(TBase));

            GeneratePrimitiveSerializers(this.knownTypes, typeof(TBase));
        }

        /// <inheritdoc />
        public object Deserialize(Stream stream, Type type)
        {
            if (!this.GetSerializerInfo(ref type, out bool isArray, out SerializerInfo info))
            {
                this.GetSerializerFor(type);
                info = this.knownTypes[type];
            }

            if (isArray)
            {
                return info.DeserializeArrayMethod(stream);
            }
            else
            {
                return info.DeserializeObjectMethod(stream);
            }
        }

        /// <inheritdoc />
        public Type GetSerializerFor(Type classType)
        {
            if (this.GetSerializerInfo(ref classType, out _, out SerializerInfo info))
            {
                if (info.SerializerType == null)
                {
                    throw new InvalidOperationException("Cycle detected generating serializer for " + classType.Name);
                }

                return info.SerializerType;
            }

            Type serializerType;
            if (IsEnum(classType))
            {
                serializerType = this.GenerateEnumSerializer(classType);
            }
            else
            {
                if (classType.GetTypeInfo().IsValueType)
                {
                    throw new InvalidOperationException("Type must be a reference type (trying to generate a serializer for " + classType.Name + ")");
                }

                // Record the fact that we're building it now (by adding it but
                // leaving it empty) the so we can detect cyclic references
                this.knownTypes.Add(classType, default);
                serializerType = this.classSerializer.GenerateFor(classType);
            }

            this.knownTypes[classType] = new SerializerInfo(serializerType);
            return serializerType;
        }

        /// <inheritdoc />
        public void Serialize(Stream stream, object value)
        {
            Type type = value.GetType();
            if (!this.GetSerializerInfo(ref type, out bool isArray, out SerializerInfo info))
            {
                this.GetSerializerFor(type);
                info = this.knownTypes[type];
            }

            if (isArray)
            {
                info.SerializeArrayMethod(stream, value);
            }
            else
            {
                info.SerializeObjectMethod(stream, value);
            }
        }

        private static void GeneratePrimitiveSerializers(
            Dictionary<Type, SerializerInfo> types,
            Type baseType)
        {
            var generator = new PrimitiveSerializerGenerator(ModuleBuilder, baseType);
            foreach (KeyValuePair<Type, Type> kvp in generator.GetSerializers())
            {
                types.Add(kvp.Key, new SerializerInfo(kvp.Value));
            }
        }

        private static bool IsEnum(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.GetTypeInfo().IsEnum;
        }

        private Type GenerateEnumSerializer(Type enumType)
        {
            if (OutputEnumNames(typeof(TBase)))
            {
                return this.enumSerializer.GenerateStringSerializer(enumType);
            }
            else
            {
                return this.enumSerializer.GenerateValueSerializer(enumType);
            }
        }

        private bool GetSerializerInfo(ref Type type, out bool isArray, out SerializerInfo info)
        {
            isArray = type.IsArray;
            if (isArray)
            {
                type = type.GetElementType();
            }

            return this.knownTypes.TryGetValue(type, out info);
        }
    }
}
