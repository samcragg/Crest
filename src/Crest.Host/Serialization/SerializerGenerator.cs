// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Contains shared data for the serializer generators.
    /// </summary>
    internal abstract class SerializerGenerator
    {
        static SerializerGenerator()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("SerializerAssembly"),
                AssemblyBuilderAccess.Run);

            ModuleBuilder = assemblyBuilder.DefineDynamicModule("Module");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerGenerator"/> class.
        /// </summary>
        protected SerializerGenerator()
        {
            // Constructor required for Sonar, along with this comment...
        }

        /// <summary>
        /// Gets or sets the module where types will be defined.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is exposed to allow unit testing.
        /// </para><para>
        /// Although the docs don't say this, the DefineType method in
        /// ModuleBuilder is thread safe, as in the Reference+GitHub source
        /// it takes a lock on a SyncRoot object.
        /// </para>
        /// </remarks>
        internal static ModuleBuilder ModuleBuilder { get; set; }

        /// <summary>
        /// Gets a value indicating whether the names of enum values should be
        /// outputted or not.
        /// </summary>
        /// <param name="serializerBase">The serializer base class type.</param>
        /// <returns>
        /// <c>true</c> to output the enum value names; <c>false</c> to output
        /// their numeric value.
        /// </returns>
        internal static bool OutputEnumNames(Type serializerBase)
        {
            const BindingFlags PublicStatic = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static;

            PropertyInfo outputEnumNames =
                serializerBase.GetProperty("OutputEnumNames", PublicStatic)
                ?? throw new InvalidOperationException(serializerBase.Name + " must contain a public static property called OutputEnumNames");

            return (bool)outputEnumNames.GetValue(null);
        }
    }
}
