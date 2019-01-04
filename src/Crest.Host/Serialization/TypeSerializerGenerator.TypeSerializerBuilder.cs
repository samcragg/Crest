// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
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
            private readonly TypeSerializerGenerator owner;

            /// <summary>
            /// Initializes a new instance of the <see cref="TypeSerializerBuilder"/> class.
            /// </summary>
            /// <param name="owner">The instance that created this class.</param>
            /// <param name="builder">The object used to create the class.</param>
            /// <param name="serializedType">
            /// The type this class will be used to serialize.
            /// </param>
            /// <param name="metadataBuilder">The metadata builder.</param>
            /// <param name="metadataField">The metadata field.</param>
            public TypeSerializerBuilder(
                TypeSerializerGenerator owner,
                TypeBuilder builder,
                Type serializedType,
                MetadataBuilder metadataBuilder,
                FieldBuilder metadataField)
            {
                this.Builder = builder;
                this.MetadataBuilder = metadataBuilder;
                this.MetadataField = metadataField;
                this.owner = owner;
                this.SerializedType = serializedType;
            }

            /// <summary>
            /// Gets the builder used for creating the class at runtime.
            /// </summary>
            public TypeBuilder Builder { get; }

            /// <summary>
            /// Gets the builder used for creating the metadata.
            /// </summary>
            public MetadataBuilder MetadataBuilder { get; }

            /// <summary>
            /// Gets the field that stores the metadata array.
            /// </summary>
            public FieldBuilder MetadataField { get; }

            /// <summary>
            /// Gets the type the generated class will serialize.
            /// </summary>
            public Type SerializedType { get; }

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

                // HACK: This should be stored in a central cache
                generatedType.GetField("_metadata_", BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, this.MetadataBuilder.GetMetadataArray(this.owner.MetadataType));

                return generatedType;
            }
        }
    }
}
