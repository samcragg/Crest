// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    /// <content>
    /// Contains the nested <see cref="BodyPart"/> struct.
    /// </content>
    internal partial class MultipartParser
    {
        /// <summary>
        /// Contains the data for a parsed part of a multipart message.
        /// </summary>
        internal struct BodyPart
        {
            /// <summary>
            /// Gets or sets the position in the stream of the end of the body.
            /// </summary>
            public int BodyEnd { get; set; }

            /// <summary>
            /// Gets or sets the position in the stream of the start of the body.
            /// </summary>
            public int BodyStart { get; set; }

            /// <summary>
            /// Gets or sets the position in the stream of the end of the header.
            /// </summary>
            public int HeaderEnd { get; set; }

            /// <summary>
            /// Gets or sets the position in the stream of the start of the header.
            /// </summary>
            public int HeaderStart { get; set; }
        }
    }
}
