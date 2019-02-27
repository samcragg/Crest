// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Util
{
    using System.Collections.Generic;
    using System.Reflection;
    using Crest.Abstractions;
    using Crest.Host.Engine;

    /// <summary>
    /// Allows the building of links from service methods.
    /// </summary>
    [SingleInstance] // We cache the service spies and delegates
    internal sealed partial class LinkProvider : ILinkProvider
    {
        private readonly Dictionary<int, MethodToUrlAdapter> adapters =
            new Dictionary<int, MethodToUrlAdapter>();

        private readonly ServiceSpy spy = new ServiceSpy();

        private delegate string MethodToUrlAdapter(object[] arguments);

        /// <inheritdoc />
        public ILinkService<T> LinkTo<T>(string relationType)
            where T : class
        {
            return new LinkService<T>(this.spy, this.GetAdapterFor, relationType);
        }

        private MethodToUrlAdapter GetAdapterFor(MethodInfo method)
        {
            lock (this.adapters)
            {
                if (!this.adapters.TryGetValue(method.MetadataToken, out MethodToUrlAdapter adapter))
                {
                    var builder = new LinkExpressionBuilder();
                    adapter = builder.FromMethod<MethodToUrlAdapter>(method);
                    this.adapters.Add(method.MetadataToken, adapter);
                }

                return adapter;
            }
        }
    }
}
