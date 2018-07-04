// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the initialization of application state during startup.
    /// </summary>
    /// <remarks>
    /// The ordering of initializers is not guaranteed, therefore, minimal
    /// dependency injection is supported when creating instances implementing
    /// this interface.
    /// </remarks>
    public interface IStartupInitializer
    {
        /// <summary>
        /// Perform the initialization.
        /// </summary>
        /// <param name="serviceRegister">Used to register services.</param>
        /// <param name="serviceLocator">Used to locate services.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method is called before any requests are accepted; request
        /// processing will only start once this method has completed.
        /// </remarks>
        Task InitializeAsync(IServiceRegister serviceRegister, IServiceLocator serviceLocator);
    }
}
