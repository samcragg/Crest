// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Reflection;

    /// <content>
    /// Contains the nested <see cref="BaseMethods"/> class.
    /// </content>
    internal sealed partial class ClassSerializerGenerator
    {
        private class BaseMethods
        {
            public BaseMethods(Type baseType, Type classSerializerInterface)
            {
                const string MetadataMethodName = "GetMetadata";
                const BindingFlags PublicStatic = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static;

                this.GetMetadata =
                    baseType.GetMethod(MetadataMethodName, PublicStatic)
                    ?? throw new InvalidOperationException(baseType.Name + " must contain a public static method called " + MetadataMethodName);

                this.GetWriter = typeof(IArraySerializer)
                    .GetProperty(nameof(IArraySerializer.Writer))
                    .GetGetMethod();

                this.WriteBeginClass = classSerializerInterface.GetMethod(
                    nameof(IClassSerializer<object>.WriteBeginClass));

                this.WriteBeginProperty = classSerializerInterface.GetMethod(
                    nameof(IClassSerializer<object>.WriteBeginProperty));

                this.WriteEndClass = classSerializerInterface.GetMethod(
                    nameof(IClassSerializer<object>.WriteEndClass));

                this.WriteEndProperty = classSerializerInterface.GetMethod(
                    nameof(IClassSerializer<object>.WriteEndProperty));
            }

            internal MethodInfo GetMetadata { get; }
            internal MethodInfo GetWriter { get; }
            internal MethodInfo WriteBeginClass { get; }
            internal MethodInfo WriteBeginProperty { get; }
            internal MethodInfo WriteEndClass { get; }
            internal MethodInfo WriteEndProperty { get; }
        }
    }
}
