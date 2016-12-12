// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the injecting of configuration properties.
    /// </summary>
    /// <remarks>
    /// The lifetime of these classes will be per application (i.e. they will
    /// be single instance) so all methods MUST be thread safe.
    /// </remarks>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Gets the relative order this should be invoked in.
        /// </summary>
        /// <remarks>
        /// Lower numbers will be invoked before higher numbers (i.e. 100 will
        /// be executed before 200).
        /// </remarks>
        int Order { get; }

        /// <summary>
        /// Allows the initialization of the provider.
        /// </summary>
        /// <param name="knownTypes">
        /// A sequence of discovered configuration classes that could be passed
        /// to <see cref="Inject(object)"/>.
        /// </param>
        /// <returns>The result of the asynchronous operation.</returns>
        Task Initialize(IEnumerable<Type> knownTypes);

        /// <summary>
        /// Injects the configuration properties into an existing object.
        /// </summary>
        /// <param name="instance">The configuration object.</param>
        void Inject(object instance);
    }
}
