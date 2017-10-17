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

    /// <summary>
    /// Generates a type for serializing data at runtime.
    /// </summary>
    internal abstract class TypeSerializerGenerator
    {
        /// <summary>
        /// Represents the flags for a public virtual method.
        /// </summary>
        protected const MethodAttributes PublicVirtualMethod =
            MethodAttributes.Final |
            MethodAttributes.HideBySig |
            MethodAttributes.NewSlot |
            MethodAttributes.Public |
            MethodAttributes.Virtual;
        private const string MetadataSuffix = "<>Metadata";
        private readonly MethodInfo getTypeMetadataMethod;
        private readonly ModuleBuilder moduleBuidler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeSerializerGenerator"/> class.
        /// </summary>
        /// <param name="module">The dynamic module to output the types to.</param>
        /// <param name="baseClass">
        /// The type for the generated classes to inherit from.
        /// </param>
        protected TypeSerializerGenerator(ModuleBuilder module, Type baseClass)
        {
            this.moduleBuidler = module;
            this.BaseClass = baseClass;
            this.StreamWriterMethods = GetStreamWriterPrimitiveMethods();

            // Can be null as it's optional
            this.getTypeMetadataMethod = baseClass.GetMethod(
                "GetTypeMetadata",
                BindingFlags.Public | BindingFlags.Static);

            Type primitiveSerializer = GetGenericInterfaceImplementation(
                baseClass.GetTypeInfo(),
                typeof(IPrimitiveSerializer<>));
            this.MetadataType = primitiveSerializer.GetGenericArguments()[0];
        }

        /// <summary>
        /// Gets the base class for the generated type.
        /// </summary>
        protected Type BaseClass { get; }

        /// <summary>
        /// Gets the type of the metadata stored for properties/types.
        /// </summary>
        protected Type MetadataType { get; }

        /// <summary>
        /// Gets the methods on the <see cref="IStreamWriter"/> that can be
        /// used to write a value to the output stream.
        /// </summary>
        protected IReadOnlyDictionary<Type, MethodInfo> StreamWriterMethods { get; }

        // /// <summary>
        // /// Gets the optional field used to store the type metadata.
        // /// </summary>
        // protected FieldInfo TypeMetadataField { get; private set; }

        /// <summary>
        /// Gets the interface implemented by a specific type.
        /// </summary>
        /// <param name="typeInfo">The type implementing the interface.</param>
        /// <param name="interfaceType">The open-generic interface.</param>
        /// <returns>The closed generic implemented interface.</returns>
        protected static Type GetGenericInterfaceImplementation(TypeInfo typeInfo, Type interfaceType)
        {
            bool FindConverterWriter(Type type, object state)
            {
                TypeInfo interfaceInfo = type.GetTypeInfo();
                return interfaceInfo.IsGenericType &&
                       interfaceInfo.GetGenericTypeDefinition() == interfaceType;
            }

            Type[] interfaces = typeInfo.FindInterfaces(FindConverterWriter, null);
            if (interfaces.Length != 1)
            {
                throw new InvalidOperationException($"{typeInfo.Name} must implement a single instance of the {interfaceType.Name} interface.");
            }

            return interfaces[0];
        }

        /// <summary>
        /// Creates a field to hold specific metadata.
        /// </summary>
        /// <param name="builder">The builder to create the field in.</param>
        /// <param name="name">The prefix for the field name.</param>
        /// <returns>A field for containing the metadata.</returns>
        protected FieldBuilder CreateMetadataField(TypeBuilder builder, string name)
        {
            return builder.DefineField(
                name + MetadataSuffix,
                this.MetadataType,
                FieldAttributes.Public | FieldAttributes.Static);
        }

        /// <summary>
        /// Creates a type based on the specified name,
        /// </summary>
        /// <param name="name">The name for the type.</param>
        /// <returns>A new builder for a class at runtime.</returns>
        protected TypeBuilder CreateType(string name)
        {
            const TypeAttributes PublicSealedClass =
                TypeAttributes.AnsiClass |
                TypeAttributes.AutoClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.Public |
                TypeAttributes.Sealed;

            TypeBuilder builder = this.moduleBuidler.DefineType(
                            this.BaseClass.Name + "<>" + name,
                            PublicSealedClass,
                            this.BaseClass);

            builder.AddInterfaceImplementation(typeof(ITypeSerializer));
            return builder;
        }

        /// <summary>
        /// Emits a constructor with the specified parameter and, optionally,
        /// specified body.
        /// </summary>
        /// <param name="builder">The builder to emit the constructor to.</param>
        /// <param name="parameter">The type of the parameter.</param>
        /// <param name="body">
        /// Optional to write additional instructions after the base class
        /// constructor has been called.
        /// </param>
        protected void EmitConstructor(
            TypeBuilder builder,
            Type parameter,
            Action<ILGenerator> body = null)
        {
            const MethodAttributes PublicConstructor =
                MethodAttributes.HideBySig |
                MethodAttributes.Public |
                MethodAttributes.RTSpecialName |
                MethodAttributes.SpecialName;

            ConstructorInfo baseConstructor = FindConstructorWithParameter(this.BaseClass, parameter);

            ConstructorBuilder constructorBuilder = builder.DefineConstructor(
                PublicConstructor,
                CallingConventions.Standard,
                new[] { parameter });

            ILGenerator generator = constructorBuilder.GetILGenerator();

            // base(arg)
            generator.EmitLoadArgument(0);
            generator.EmitLoadArgument(1);
            generator.Emit(OpCodes.Call, baseConstructor);

            body?.Invoke(generator);
            generator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits a call to a method that expects the metadata for the type or
        /// null if the base serializer doesn't provide such data.
        /// </summary>
        /// <param name="builder">The builder to emit the metadata field to.</param>
        /// <param name="generator">Where to emit the instructions to.</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="classType">The type to generate the metadata of.</param>
        protected void EmitWriteBeginTypeMetadata(
            TypeBuilder builder,
            ILGenerator generator,
            MethodInfo method,
            Type classType)
        {
            generator.EmitLoadArgument(0);

            if (this.getTypeMetadataMethod == null)
            {
                generator.Emit(OpCodes.Ldnull);
            }
            else
            {
                FieldBuilder field = this.CreateMetadataField(builder, classType.Name);
                generator.Emit(OpCodes.Ldsfld, field);
            }

            generator.EmitCall(this.BaseClass, method);
        }

        /// <summary>
        /// Builds the type and initializes the static metadata.
        /// </summary>
        /// <param name="builder">The type builder.</param>
        /// <param name="classType">
        /// The type the serializer has been generated for.
        /// </param>
        /// <returns>The created type.</returns>
        protected Type GenerateType(TypeBuilder builder, Type classType)
        {
            Type generatedType = builder.CreateTypeInfo().AsType();
            this.InitializeTypeMetadata(generatedType, classType);
            return generatedType;
        }

        /// <summary>
        /// Initializes the static type metadata field for the type.
        /// </summary>
        /// <param name="generatedType">The serializer.</param>
        /// <param name="type">The type being serialized.</param>
        protected void InitializeTypeMetadata(Type generatedType, Type type)
        {
            if (this.getTypeMetadataMethod != null)
            {
                object metadata = this.getTypeMetadataMethod.Invoke(
                    null,
                    new object[] { type });

                generatedType.GetField(type.Name + MetadataSuffix)
                             .SetValue(null, metadata);
            }
        }

        private static ConstructorInfo FindConstructorWithParameter(Type classType, Type parameterType)
        {
            bool HasMatchingParameters(ConstructorInfo constructor)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                return (parameters.Length == 1) &&
                       (parameters[0].ParameterType == parameterType);
            }

            bool IsVisibleToDerivedTypes(ConstructorInfo constructor)
            {
                return constructor.IsPublic || constructor.IsFamily;
            }

            ConstructorInfo[] constructors = classType.GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return constructors
                .Where(IsVisibleToDerivedTypes)
                .FirstOrDefault(HasMatchingParameters)
                ?? throw new InvalidOperationException(classType.Name + " must contain a constructor accessible to a derived class that has a parameter of type " + parameterType.Name);
        }

        private static IReadOnlyDictionary<Type, MethodInfo> GetStreamWriterPrimitiveMethods()
        {
            return typeof(IStreamWriter).GetTypeInfo()
                .DeclaredMethods
                .Where(m => m.Name.StartsWith("Write", StringComparison.Ordinal))
                .Where(m => m.Name != nameof(IStreamWriter.WriteObject) && m.Name != nameof(IStreamWriter.WriteNull))
                .ToDictionary(m => m.GetParameters().Single().ParameterType);
        }
    }
}
