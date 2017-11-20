// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    /// <summary>
    /// Indicates whether an object is being read or written to a stream.
    /// </summary>
    public enum SerializationMode
    {
        /// <summary>
        /// The object should be read from the stream.
        /// </summary>
        Deserialize = 1,

        /// <summary>
        /// The object should be written to the stream.
        /// </summary>
        Serialize = 2
    }
}
