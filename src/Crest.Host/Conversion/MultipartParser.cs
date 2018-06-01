// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Tokens = HttpHeaderParser.Tokens;

    /// <summary>
    /// Parses the body of multipart requests per RFC 2046.
    /// </summary>
    internal sealed partial class MultipartParser
    {
        private const string InvalidBody = "Invalid multipart request body";
        private readonly byte[] boundary;
        private readonly Stream stream;
        private int currentByte;
        private BodyPart currentPart;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipartParser"/> class.
        /// </summary>
        /// <param name="boundary">The boundary string.</param>
        /// <param name="body">The message body.</param>
        public MultipartParser(string boundary, Stream body)
        {
            this.boundary = Encoding.ASCII.GetBytes(boundary);
            this.stream = body;
        }

        private bool IsEndOfStream => this.currentByte == -1;

        /// <summary>
        /// Gets the separate parts of the message body.
        /// </summary>
        /// <returns>A sequence of message parts.</returns>
        public IEnumerable<BodyPart> Parse()
        {
            if (!this.ReadMultipartBodyStart())
            {
                throw new FormatException(InvalidBody);
            }

            yield return this.currentPart;
            while (this.ReadEncapsulation())
            {
                yield return this.currentPart;
            }

            if (!this.ReadCloseDelimiter())
            {
                throw new FormatException(InvalidBody);
            }
        }

        private int DiscardText()
        {
            // discard-text := *(*text CRLF) *text
            //                 ; May be ignored or discarded.
            int count = 0;
            do
            {
                int previous = this.currentByte;
                this.ReadByte();
                count++;
                if ((this.currentByte == Tokens.LF) && (previous == Tokens.CR))
                {
                    break;
                }
            }
            while (!this.IsEndOfStream);

            return count;
        }

        private bool ReadBodyPart()
        {
            // delimiter := CRLF dash-boundary
            // body-part := MIME-part-headers [CRLF *OCTET]
            //              ; Lines in a body-part must not start
            //              ; with the specified dash-boundary and
            //              ; the delimiter must not appear anywhere
            //              ; in the body part.  Note that the
            //              ; semantics of a body-part differ from
            //              ; the semantics of a message, as
            //              ; described in the text.
            BodyPart part = default;
            part.HeaderStart = (int)this.stream.Position;
            do
            {
                // Discard text will read up to and including the CRLF,
                // therefore if we find a dash-boundary it would have been
                // preceded by a CRLF, thus classed as a delimiter
                int lineLength = this.DiscardText();
                if (this.IsEndOfStream)
                {
                    return false;
                }

                if (part.HeaderEnd == 0)
                {
                    // Is the line empty (CRLF)?
                    if (lineLength <= 2)
                    {
                        part.BodyStart = (int)this.stream.Position;
                        part.HeaderEnd = part.BodyStart - 2; // -2 for the CRLF
                    }
                }
                else
                {
                    part.BodyEnd = (int)this.stream.Position - 2; // -2 for the CRLF
                }
            }
            while (!this.ReadDashBoundary());

            this.currentPart = part;
            return true;
        }

        private int ReadByte()
        {
            return this.currentByte = this.stream.ReadByte();
        }

        private bool ReadCloseDelimiter()
        {
            // close-delimiter := delimiter "--"
            //
            // Before this method is called we have already read the delimiter
            return (this.currentByte == '-') && (this.ReadByte() == '-');
        }

        private bool ReadDashBoundary()
        {
            // dash-boundary := "--" boundary
            if ((this.ReadByte() != '-') || (this.ReadByte() != '-'))
            {
                return false;
            }

            for (int i = 0; i < this.boundary.Length; i++)
            {
                if (this.ReadByte() != this.boundary[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool ReadEncapsulation()
        {
            // encapsulation := delimiter transport-padding
            //                  CRLF body-part
            //
            // Before this method is called we've already read a delimiter
            this.TransportPadding();
            if (!this.ReadNewLine())
            {
                return false;
            }

            return this.ReadBodyPart();
        }

        private bool ReadMultipartBodyStart()
        {
            // multipart-body := [preamble CRLF]
            //                   dash-boundary transport-padding CRLF
            //                   body-part *encapsulation
            //                   close-delimiter transport-padding
            //                   [CRLF epilogue]
            //
            // We're just parsing up to the first encapsulation (i.e. finishing
            // after we've found a body-part)
            while (!this.ReadDashBoundary())
            {
                this.DiscardText();
                if (this.IsEndOfStream)
                {
                    return false;
                }
            }

            this.TransportPadding();
            return this.ReadNewLine() && this.ReadBodyPart();
        }

        private bool ReadNewLine()
        {
            if (this.currentByte != Tokens.CR)
            {
                return false;
            }

            return this.ReadByte() == Tokens.LF;
        }

        private void TransportPadding()
        {
            bool IsLwspChar(int value)
            {
                // RFC 822:
                // LWSP-char = SPACE / HTAB
                return (value == Tokens.Space) || (value == Tokens.HTab);
            }

            // transport-padding := *LWSP-char
            //                      ; Composers MUST NOT generate
            //                      ; non-zero length transport
            //                      ; padding, but receivers MUST
            //                      ; be able to handle padding
            //                      ; added by message transports.
            do
            {
                this.ReadByte();
            }
            while (IsLwspChar(this.currentByte));
        }
    }
}
