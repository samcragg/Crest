// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides methods to create <see cref="MappingInfo"/>s.
    /// </summary>
    public interface IMappingInfoFactory
    {
        /// <summary>
        /// Creates a sequence of mapping information.
        /// </summary>
        /// <returns>A sequence of mapping information.</returns>
        IEnumerable<MappingInfo> GetMappingInformation();
    }
}
