// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Util
{
    using System;
    using System.Reflection;
    using Crest.Abstractions;
    using Crest.Core;

    /// <summary>
    /// Contains the nested <see cref="LinkService{T}"/> class.
    /// </summary>
    internal partial class LinkProvider
    {
        private class LinkService<T> : ILinkService<T>
            where T : class
        {
            private readonly Func<MethodInfo, MethodToUrlAdapter> adapterFactory;
            private readonly string relation;
            private readonly ServiceSpy spy;

            internal LinkService(
                ServiceSpy spy,
                Func<MethodInfo, MethodToUrlAdapter> adapterFactory,
                string relation)
            {
                this.adapterFactory = adapterFactory;
                this.relation = relation;
                this.spy = spy;
            }

            public LinkBuilder Calling(Action<T> action)
            {
                (MethodInfo method, object[] args) = this.spy.InvokeAction(action);
                MethodToUrlAdapter adapter = this.adapterFactory(method);
                string uri = adapter(args);
                return new LinkBuilder(this.relation, new Uri(uri, UriKind.Relative));
            }
        }
    }
}
