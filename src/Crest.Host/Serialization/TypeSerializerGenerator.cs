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
    using Crest.Host.Serialization.Internal;

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
        private readonly List<FieldBuilder> fields = new List<FieldBuilder>();
        private readonly ModuleBuilder moduleBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeSerializerGenerator"/> class.
        /// </summary>
        /// <param name="module">The dynamic module to output the types to.</param>
        /// <param name="baseClass">
        /// The type for the generated classes to inherit from.
        /// </param>
        protected TypeSerializerGenerator(ModuleBuilder module, Type baseClass)
        {
            this.moduleBuilder = module;
            this.BaseClass = baseClass;
            this.Methods = new Methods(baseClass);

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
        /// Gets the builder created by the last call to <see cref="CreateType(string)"/>.
        /// </summary>
        protected TypeBuilder Builder { get; private set; }

        /// <summary>
        /// Gets the type of the metadata stored for properties/types.
        /// </summary>
        protected Type MetadataType { get; }

        /// <summary>
        /// Gets the method metadata for commonly used methods called by the
        /// generated code.
        /// </summary>
        protected Methods Methods { get; }

        /// <summary>
        /// Gets the interface implemented by a specific type.
        /// </summary>
        /// <param name="typeInfo">The type implementing the interface.</param>
        /// <param name="interfaceType">The open-generic interface.</param>
        /// <returns>The closed generic implemented interface.</returns>
        internal static Type GetGenericInterfaceImplementation(TypeInfo typeInfo, Type interfaceType)
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
        /// Determines whether the specified type can contain a <c>null</c> value.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>
        /// <c>true</c> if <c>null</c> is a valid value for the type; otherwise,
        /// <c>false</c>.
        /// </returns>
        protected static bool CanBeNull(Type type)
        {
            return !type.GetTypeInfo().IsValueType ||
                   (Nullable.GetUnderlyingType(type) != null);
        }

        /// <summary>
        /// Creates a field to hold specific metadata.
        /// </summary>
        /// <param name="name">The prefix for the field name.</param>
        /// <returns>A field for containing the metadata.</returns>
        protected FieldBuilder CreateMetadataField(string name)
        {
            name = name + MetadataSuffix;
            FieldBuilder field = this.fields.FirstOrDefault(
                f => string.Equals(f.Name, name, StringComparison.Ordinal));

            if (field == null)
            {
                field = this.Builder.DefineField(
                    name,
                    this.MetadataType,
                    FieldAttributes.Public | FieldAttributes.Static);

                this.fields.Add(field);
            }

            return field;
        }

        /// <summary>
        /// Creates a type based on the specified name,
        /// </summary>
        /// <param name="name">The name for the type.</param>
        protected void CreateType(string name)
        {
            const TypeAttributes PublicSealedClass =
                TypeAttributes.AnsiClass |
                TypeAttributes.AutoClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.Public |
                TypeAttributes.Sealed;

            TypeBuilder builder = this.moduleBuilder.DefineType(
                            this.BaseClass.Name + "<>" + name,
                            PublicSealedClass,
                            this.BaseClass);

            builder.AddInterfaceImplementation(typeof(ITypeSerializer));

            this.fields.Clear();
            this.Builder = builder;
        }

        /// <summary>
        /// Emits a call to a method that expects the metadata for the type or
        /// null if the base serializer doesn't provide such data.
        /// </summary>
        /// <param name="generator">Where to emit the instructions to.</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="classType">The type to generate the metadata of.</param>
        protected void EmitCallBeginMethodWithTypeMetadata(
            ILGenerator generator,
            MethodInfo method,
            Type classType)
        {
            generator.EmitLoadArgument(0);

            if (this.Methods.BaseClass.GetTypeMetadata == null)
            {
                generator.Emit(OpCodes.Ldnull);
            }
            else
            {
                FieldBuilder field = this.CreateMetadataField(classType.Name);
                generator.Emit(OpCodes.Ldsfld, field);
            }

            generator.EmitCall(this.BaseClass, method);
        }

        /// <summary>
        /// Emits a constructor with the specified parameter and, optionally,
        /// specified body.
        /// </summary>
        /// <param name="body">
        /// Used to write additional instructions after the base class
        /// constructor has been called, can be <c>null</c>.
        /// </param>
        /// <param name="parameters">The types of the parameter.</param>
        protected void EmitConstructor(
            Action<ILGenerator> body,
            params Type[] parameters)
        {
            const MethodAttributes PublicConstructor =
                MethodAttributes.HideBySig |
                MethodAttributes.Public |
                MethodAttributes.RTSpecialName |
                MethodAttributes.SpecialName;

            ConstructorInfo baseConstructor = FindConstructorWithParameters(this.BaseClass, parameters);

            ConstructorBuilder constructorBuilder = this.Builder.DefineConstructor(
                PublicConstructor,
                CallingConventions.Standard,
                parameters);

            ILGenerator generator = constructorBuilder.GetILGenerator();

            // base(arg...)
            generator.EmitLoadArgument(0);
            for (int i = 0; i < parameters.Length; i++)
            {
                generator.EmitLoadArgument(i + 1);
            }

            generator.Emit(OpCodes.Call, baseConstructor);

            body?.Invoke(generator);
            generator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Builds the type and initializes the static metadata.
        /// </summary>
        /// <param name="classType">
        /// The type the serializer has been generated for.
        /// </param>
        /// <returns>The created type.</returns>
        protected Type GenerateType(Type classType)
        {
            Type generatedType = this.Builder.CreateTypeInfo().AsType();
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
            if (this.Methods.BaseClass.GetTypeMetadata != null)
            {
                object metadata = this.Methods.BaseClass.GetTypeMetadata.Invoke(
                    null,
                    new object[] { type });

                generatedType.GetField(type.Name + MetadataSuffix)
                             .SetValue(null, metadata);
            }
        }

        private static ConstructorInfo FindConstructorWithParameters(Type classType, Type[] parameterTypes)
        {
            bool HasMatchingParameters(ConstructorInfo constructor)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                return parameters.Select(p => p.ParameterType).SequenceEqual(parameterTypes);
            }

            bool IsVisibleToDerivedTypes(ConstructorInfo constructor)
            {
                return constructor.IsPublic || constructor.IsFamily;
            }

            string GetErrorMessage()
            {
                return classType.Name +
                    " must contain a constructor accessible to a derived class that has a parameters of types " +
                    string.Join(", ", parameterTypes.Select(p => p.Name));
            }

            ConstructorInfo[] constructors = classType.GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return constructors
                .Where(IsVisibleToDerivedTypes)
                .FirstOrDefault(HasMatchingParameters)
                ?? throw new InvalidOperationException(GetErrorMessage());
        }
    }
}
