// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess
{
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.DataAccess.Expressions;

    /// <summary>
    /// Initializes the mapping cache.
    /// </summary>
    internal sealed class CacheInitializer : IStartupInitializer
    {
        /// <inheritdoc />
        public Task InitializeAsync(IServiceRegister serviceRegister, IServiceLocator serviceLocator)
        {
            QueryableExtensions.MappingCache = (MappingCache)serviceLocator.GetService(
                typeof(MappingCache));

            return Task.CompletedTask;
        }
    }
}
