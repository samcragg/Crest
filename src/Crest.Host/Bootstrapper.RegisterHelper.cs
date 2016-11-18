// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Host.Engine;
    using DryIoc;

    /// <content>
    /// Contains the nested helper <see cref="RegisterHelper"/> class.
    /// </content>
    public abstract partial class Bootstrapper
    {
        private class RegisterHelper
        {
            private readonly IDiscoveryService discovery;

            internal RegisterHelper(IDiscoveryService discovery)
            {
                this.discovery = discovery;
            }

            internal void RegisterMany(IRegistrator container, IEnumerable<Type> types)
            {
                container.RegisterMany(
                    types.Where(IsImplementationType),
                    this.TryRegisterMany,
                    nonPublicServiceTypes: true);
            }

            private static bool IsImplementationType(Type type)
            {
                TypeInfo typeInfo = type.GetTypeInfo();
                return typeInfo.IsClass && !typeInfo.IsAbstract;
            }

            private void TryRegisterMany(IRegistrator registrator, Type[] serviceTypes, Type implementingType)
            {
                IReuse reuse =
                    serviceTypes.Any(this.discovery.IsSingleInstance) ?
                        Reuse.Singleton : Reuse.Transient;

                registrator.RegisterMany(
                    serviceTypes,
                    implementingType,
                    reuse,
                    ifAlreadyRegistered: IfAlreadyRegistered.Keep,
                    made: FactoryMethod.ConstructorWithResolvableArguments,
                    setup: Setup.With(trackDisposableTransient: true));
            }
        }
    }
}
