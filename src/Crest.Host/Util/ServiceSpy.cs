// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Util
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Crest.Host.Util.Internal;

    /// <summary>
    /// Allows the recording of method invocations on an interface.
    /// </summary>
    internal sealed class ServiceSpy
    {
        private readonly Dictionary<Type, ServiceProxy> proxies = new Dictionary<Type, ServiceProxy>();

        /// <summary>
        /// Invoke the specified function on an interface.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="action">The function to invoke.</param>
        /// <returns>
        /// The method that was called, along with the arguments passed to it.
        /// </returns>
        public (MethodInfo method, object[] args) InvokeAction<T>(Action<T> action)
        {
            ServiceProxy proxy = this.GetProxyFor<T>();
            lock (proxy)
            {
                action((T)(object)proxy);
                if (proxy.CalledMethod == null)
                {
                    throw new InvalidOperationException("No method was invoked on service.");
                }

                (MethodInfo, object[]) result = (proxy.CalledMethod, proxy.Arguments);
                proxy.Clear();
                return result;
            }
        }

        private ServiceProxy GetProxyFor<T>()
        {
            lock (this.proxies)
            {
                if (!this.proxies.TryGetValue(typeof(T), out ServiceProxy proxy))
                {
                    object dispatchProxy = DispatchProxy.Create<T, ServiceProxy>();
                    proxy = (ServiceProxy)dispatchProxy;
                    this.proxies.Add(typeof(T), proxy);
                }

                return proxy;
            }
        }
    }
}
