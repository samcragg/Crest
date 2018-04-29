// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;

    /// <content>
    /// Contains the nested <see cref="TypeInitializer"/> class.
    /// </content>
    internal sealed partial class JsonConfigurationProvider
    {
        private class TypeInitializer
        {
            public TypeInitializer(Type type)
            {
                this.EnvironmentSettings = DoNothing;
                this.GlobalSettings = DoNothing;
                this.Type = type;
            }

            public static Action<object> DoNothing { get; } = EmptyMethod;

            public Action<object> EnvironmentSettings { get; set; }

            public Action<object> GlobalSettings { get; set; }

            public Type Type { get; }

            private static void EmptyMethod(object instance)
            {
                // This method is intentionally empty to allow the actions
                // to always be invoked
            }
        }
    }
}
