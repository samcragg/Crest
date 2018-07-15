// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Generates a type for serializing data at runtime.
    /// </summary>
    internal abstract partial class TypeSerializerGenerator
    {
        private const string MetadataSuffix = "<>Metadata";
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
        /// Creates a type based on the specified name.
        /// </summary>
        /// <param name="serializedType">The type that will be serialized.</param>
        /// <param name="name">The name for the type.</param>
        /// <returns>A new builder for a class at runtime.</returns>
        protected TypeSerializerBuilder CreateType(Type serializedType, string name)
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

            return new TypeSerializerBuilder(this, builder, serializedType);
        }

        /// <summary>
        /// Emits a call to a method that expects the metadata for the type or
        /// null if the base serializer doesn't provide such data.
        /// </summary>
        /// <param name="builder">The builder to emit the metadata field to.</param>
        /// <param name="generator">Where to emit the instructions to.</param>
        /// <param name="method">The method to invoke.</param>
        protected void EmitCallBeginMethodWithTypeMetadata(
            TypeSerializerBuilder builder,
            ILGenerator generator,
            MethodInfo method)
        {
            generator.EmitLoadArgument(0);

            if (this.Methods.BaseClass.GetTypeMetadata == null)
            {
                generator.Emit(OpCodes.Ldnull);
            }
            else
            {
                FieldBuilder field = builder.CreateMetadataField(builder.SerializedType.Name);
                generator.Emit(OpCodes.Ldsfld, field);
            }

            generator.EmitCall(this.BaseClass, method);
        }

        /// <summary>
        /// Emits a constructor with the specified parameter and, optionally,
        /// specified body.
        /// </summary>
        /// <param name="builder">The builder to emit the constructor to.</param>
        /// <param name="body">
        /// Used to write additional instructions after the base class
        /// constructor has been called, can be <c>null</c>.
        /// </param>
        /// <param name="parameters">The types of the parameter.</param>
        protected void EmitConstructor(
            TypeSerializerBuilder builder,
            Action<ILGenerator> body,
            params Type[] parameters)
        {
            const MethodAttributes PublicConstructor =
                MethodAttributes.HideBySig |
                MethodAttributes.Public |
                MethodAttributes.RTSpecialName |
                MethodAttributes.SpecialName;

            ConstructorInfo baseConstructor = FindConstructorWithParameters(this.BaseClass, parameters);

            ConstructorBuilder constructorBuilder = builder.Builder.DefineConstructor(
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
