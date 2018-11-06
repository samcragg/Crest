// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Util.Internal
{
    using System;
    using System.ComponentModel;
    using System.Reflection;

    /// <summary>
    /// Used to create a proxy for a type and record its method invocations.
    /// </summary>
    /// <remarks>
    /// This class MUST be public for <see cref="DispatchProxy"/> to be able to
    /// create the proxies.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ServiceProxy : DispatchProxy
    {
        /// <summary>
        /// Gets the arguments to the last invocation.
        /// </summary>
        internal object[] Arguments { get; private set; }

        /// <summary>
        /// Gets the method that was called.
        /// </summary>
        internal MethodInfo CalledMethod { get; private set; }

        /// <summary>
        /// Clears the recorded method information.
        /// </summary>
        internal void Clear()
        {
            this.Arguments = null;
            this.CalledMethod = null;
        }

        /// <inheritdoc />
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (this.CalledMethod != null)
            {
                throw new InvalidOperationException("Cannot invoke multiple methods on service.");
            }

            this.Arguments = args;
            this.CalledMethod = targetMethod;
            return null;
        }
    }
}
