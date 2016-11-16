// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using Crest.Host.Routing;
    using DryIoc;

    /// <content>
    /// Contains the nested helper <see cref="ContainerAdapter"/> class.
    /// </content>
    public abstract partial class Bootstrapper
    {
        private class ContainerAdapter : IServiceProvider
        {
            internal ContainerAdapter()
            {
                this.Container = new Container();
            }

            internal Container Container { get; }

            public object GetService(Type serviceType)
            {
                return this.Container.Resolve(serviceType);
            }
        }
    }
}
