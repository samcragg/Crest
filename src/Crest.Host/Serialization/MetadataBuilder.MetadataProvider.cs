// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Reflection;

    /// <content>
    /// Contains the nested helper <see cref="MetadataProvider{T}"/> class.
    /// </content>
    internal partial class MetadataBuilder
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S2743:Static fields should not be used in generic types",
            Justification = "We want the static data per different type, therefore this is the desired behaviour")]
        private static class MetadataProvider<T>
        {
            public static Func<PropertyInfo, object> PropertyMetadataAdapter { get; }
                = GetPropertyMetadata();

            public static Func<Type, object> TypeMetadataAdapter { get; }
                = GetTypeMetadata();

            private static Func<PropertyInfo, object> GetPropertyMetadata()
            {
                MethodInfo method = typeof(T).GetMethod(MetadataMethodName, PublicStatic)
                    ?? throw new InvalidOperationException(
                        typeof(T).Name + " must contain a public static method called " + MetadataMethodName);

                return (Func<PropertyInfo, object>)method.CreateDelegate(
                    typeof(Func<PropertyInfo, object>));
            }

            private static Func<Type, object> GetTypeMetadata()
            {
                MethodInfo method = typeof(T).GetMethod(TypeMetadataMethodName, PublicStatic);
                if (method == null)
                {
                    return _ => null;
                }
                else
                {
                    return (Func<Type, object>)method.CreateDelegate(
                        typeof(Func<Type, object>));
                }
            }
        }
    }
}
