// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using Crest.Abstractions;

    /// <content>
    /// Contains the nested <see cref="Routes"/> class.
    /// </content>
    internal sealed partial class RouteMapper
    {
        private class Routes
        {
            public Routes()
            {
                this.Overrides = new StringDictionary<OverrideMethod>();
                this.Root = new RouteNode<Versions>(new VersionCaptureNode());
            }

            public StringDictionary<OverrideMethod> Overrides { get; }

            public RouteNode<Versions> Root { get; }
        }
    }
}
