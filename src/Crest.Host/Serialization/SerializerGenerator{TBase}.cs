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
    using System.Reflection.Emit;

    /// <summary>
    /// Generates serializers at runtime for specific types.
    /// </summary>
    /// <typeparam name="TBase">
    /// The type of the base class the generated serializers will inherit.
    /// </typeparam>
    internal sealed partial class SerializerGenerator<TBase> : SerializerGenerator, ISerializerGenerator<TBase>
    {
        private readonly ClassSerializerGenerator classSerializer;

        // This stores all the enum generators. They are stored base on the
        // TypeCode of the underlying type of the enum. This will be a single
        // array if the base serializer class outputs the enum names, as we
        // call object.ToString on those so one size fits all.
        private readonly SerializerInfo[] enumSerializers;

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

            this.enumSerializers = GenerateEnumSerializers();
            GeneratePrimitiveSerializers(this.knownTypes, typeof(TBase));
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

            if (classType.GetTypeInfo().IsValueType)
            {
                throw new InvalidOperationException("Type must be a reference type (trying to generate a serializer for " + classType.Name + ")");
            }

            // Record the fact that we're building it now (by adding it but
            // leaving it empty) the so we can detect cyclic references
            this.knownTypes.Add(classType, default(SerializerInfo));

            Type serializer = this.classSerializer.GenerateFor(classType);
            this.knownTypes[classType] = new SerializerInfo(serializer);
            return serializer;
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

        private static SerializerInfo[] GenerateEnumSerializers()
        {
            var generator = new EnumSerializerGenerator(ModuleBuilder, typeof(TBase));
            if (OutputEnumNames(typeof(TBase)))
            {
                return new[]
                {
                    new SerializerInfo(generator.GenerateStringSerializer())
                };
            }

            var results = new SerializerInfo[TypeCode.UInt64 - TypeCode.SByte];
            for (int i = 0; i < results.Length; i++)
            {
                TypeCode typeCode = TypeCode.SByte + i;
                var primitive = Type.GetType("System." + typeCode);
                results[i] = new SerializerInfo(generator.GenerateValueSerializer(primitive));
            }

            return results;
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

        private SerializerInfo GetEnumeratorSerializer(Type type)
        {
            // If we only have one then it can be used for any type, as it will
            // call object.ToString
            if (this.enumSerializers.Length == 1)
            {
                return this.enumSerializers[0];
            }
            else
            {
                // An enum can be any numeric type except char, the first
                // being SByte
                Type primitive = Enum.GetUnderlyingType(type);
                int index = Type.GetTypeCode(primitive) - TypeCode.SByte;
                return this.enumSerializers[index];
            }
        }

        private bool GetSerializerInfo(ref Type type, out bool isArray, out SerializerInfo info)
        {
            isArray = type.IsArray;
            if (isArray)
            {
                type = type.GetElementType();
            }

            if (type.GetTypeInfo().IsEnum)
            {
                info = this.GetEnumeratorSerializer(type);
                return true;
            }
            else
            {
                return this.knownTypes.TryGetValue(type, out info);
            }
        }
    }
}
