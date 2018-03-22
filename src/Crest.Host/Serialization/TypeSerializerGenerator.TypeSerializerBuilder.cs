// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <content>
    /// Contains the nested <see cref="TypeSerializerBuilder"/> class.
    /// </content>
    internal abstract partial class TypeSerializerGenerator
    {
        /// <summary>
        /// Helper methods for building a type at runtime.
        /// </summary>
        protected sealed class TypeSerializerBuilder
        {
            private readonly List<FieldBuilder> fields = new List<FieldBuilder>();
            private readonly TypeSerializerGenerator owner;

            /// <summary>
            /// Initializes a new instance of the <see cref="TypeSerializerBuilder"/> class.
            /// </summary>
            /// <param name="owner">The instance that created this class.</param>
            /// <param name="builder">The object used to create the class.</param>
            /// <param name="serializedType">
            /// The type this class will be used to serialize.
            /// </param>
            public TypeSerializerBuilder(
                TypeSerializerGenerator owner,
                TypeBuilder builder,
                Type serializedType)
            {
                this.Builder = builder;
                this.owner = owner;
                this.SerializedType = serializedType;
            }

            /// <summary>
            /// Gets the builder used for creating the class at runtime.
            /// </summary>
            public TypeBuilder Builder { get; }

            /// <summary>
            /// Gets the fields that have been added to the type.
            /// </summary>
            public IReadOnlyList<FieldInfo> Fields => this.fields;

            /// <summary>
            /// Gets the type the generated class will serialize.
            /// </summary>
            public Type SerializedType { get; }

            /// <summary>
            /// Creates a field to hold specific metadata.
            /// </summary>
            /// <param name="name">The prefix for the field name.</param>
            /// <returns>A field for containing the metadata.</returns>
            public FieldBuilder CreateMetadataField(string name)
            {
                name = name + MetadataSuffix;
                FieldBuilder field = this.fields.FirstOrDefault(
                    f => string.Equals(f.Name, name, StringComparison.Ordinal));

                if (field == null)
                {
                    field = this.Builder.DefineField(
                        name,
                        this.owner.MetadataType,
                        FieldAttributes.Public | FieldAttributes.Static);

                    this.fields.Add(field);
                }

                return field;
            }

            /// <summary>
            /// Adds a new method to the type.
            /// </summary>
            /// <param name="name">The name of the method.</param>
            /// <returns>The builder for the generated method.</returns>
            public MethodBuilder CreatePublicVirtualMethod(string name)
            {
                const MethodAttributes PublicVirtualMethod =
                        MethodAttributes.Final |
                        MethodAttributes.HideBySig |
                        MethodAttributes.NewSlot |
                        MethodAttributes.Public |
                        MethodAttributes.Virtual;

                return this.Builder.DefineMethod(
                    name,
                    PublicVirtualMethod,
                    CallingConventions.HasThis);
            }

            /// <summary>
            /// Builds the type and initializes the static metadata.
            /// </summary>
            /// <returns>The created type.</returns>
            public Type GenerateType()
            {
                Type generatedType = this.Builder.CreateTypeInfo().AsType();
                this.InitializeTypeMetadata(generatedType);
                return generatedType;
            }

            private void InitializeTypeMetadata(Type generatedType)
            {
                MethodInfo getTypeMetadata = this.owner.Methods.BaseClass.GetTypeMetadata;
                if (getTypeMetadata != null)
                {
                    object metadata = getTypeMetadata.Invoke(
                        null,
                        new object[] { this.SerializedType });

                    generatedType.GetField(this.SerializedType.Name + MetadataSuffix)
                                 .SetValue(null, metadata);
                }
            }
        }
    }
}
