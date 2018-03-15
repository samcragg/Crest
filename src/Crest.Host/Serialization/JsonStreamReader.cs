// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.IO;
    using Crest.Host.Conversion;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Allows the reading of values from a stream representing JSON data.
    /// </summary>
    internal sealed partial class JsonStreamReader : ValueReader, IDisposable
    {
        private readonly StringBuffer stringBuffer = new StringBuffer();
        private StreamIterator iterator;
        private int startPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read the values from.</param>
        public JsonStreamReader(Stream stream)
        {
            this.iterator = new StreamIterator(stream);

            // The iterator always points to the start of the next token to be
            // read, so move it along now. If it moves to the end then the
            // Current property will be cleared so the error checking in the
            // methods below will pick it up
            this.iterator.MoveNext();
        }

        /// <summary>
        /// Releases the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            this.iterator.Dispose();
            this.stringBuffer.Dispose();
        }

        /// <inheritdoc />
        public override bool ReadBoolean()
        {
            this.SkipWhiteSpace();

            char c = this.iterator.Current;
            if (c == 'f')
            {
                this.Expect("alse");
                return false;
            }
            else if (c == 't')
            {
                this.Expect("rue");
                return true;
            }
            else
            {
                throw this.CreateUnexpectedTokenException();
            }
        }

        /// <inheritdoc />
        public override char ReadChar()
        {
            this.SkipWhiteSpace();

            this.Expect('"');
            char c = JsonStringEncoding.DecodeChar(this.iterator);
            this.Expect('"');
            return c;
        }

        /// <inheritdoc />
        public override decimal ReadDecimal()
        {
            this.SkipWhiteSpace();

            ParseResult<decimal> result =
                DecimalConverter.TryReadDecimal(this.iterator.GetSpan(128));

            return this.ProcessResult(result, "number");
        }

        /// <inheritdoc />
        public override double ReadDouble()
        {
            this.SkipWhiteSpace();

            ParseResult<double> result =
                DoubleConverter.TryReadDouble(this.iterator.GetSpan(512));

            return this.ProcessResult(result, "number");
        }

        /// <inheritdoc />
        public override bool ReadNull()
        {
            this.SkipWhiteSpace();

            if (this.iterator.Current != 'n')
            {
                return false;
            }

            this.Expect("ull");
            return true;
        }

        /// <inheritdoc />
        public override string ReadString()
        {
            this.ReadStringIntoBuffer();
            return this.stringBuffer.ToString();
        }

        /// <summary>
        /// Ensures the specified JSON token is in the stream and moves past it,
        /// ignoring surrounding whitespace.
        /// </summary>
        /// <param name="c">The token character.</param>
        internal void ExpectToken(char c)
        {
            this.SkipWhiteSpace();
            this.Expect(c);
        }

        /// <inheritdoc />
        internal override string GetCurrentPosition()
        {
            return "character " + this.startPosition;
        }

        /// <summary>
        /// Reads the first character of the next JSON token, skipping any
        /// whitespace, without consuming the token.
        /// </summary>
        /// <returns>
        /// The start of the token, or <c>'\0'</c> if at the end of the stream.
        /// </returns>
        internal char PeekToken()
        {
            this.SkipWhiteSpace();
            return this.iterator.Current;
        }

        /// <summary>
        /// Reads and consumes the specified character, skipping any whitespace.
        /// </summary>
        /// <param name="c">The character to advance over.</param>
        /// <returns>
        /// <c>true</c> if the token was read; otherwise, <c>false</c>.
        /// </returns>
        internal bool TryReadToken(char c)
        {
            this.SkipWhiteSpace();

            if (this.iterator.Current != c)
            {
                return false;
            }
            else
            {
                this.iterator.MoveNext();
                return true;
            }
        }

        /// <inheritdoc />
        protected override long ReadSignedInt(long min, long max)
        {
            this.SkipWhiteSpace();

            ParseResult<long> result = IntegerConverter.TryReadSignedInt(
                this.iterator.GetSpan(128),
                min,
                max);

            return this.ProcessResult(result, "number");
        }

        /// <inheritdoc />
        protected override ulong ReadUnsignedInt(ulong max)
        {
            this.SkipWhiteSpace();

            ParseResult<ulong> result = IntegerConverter.TryReadUnsignedInt(
                this.iterator.GetSpan(128),
                max);

            return this.ProcessResult(result, "number");
        }

        private static bool IsWhiteSpace(char b)
        {
            return (b == ' ') || (b == '\t') || (b == '\n') || (b == '\r');
        }

        /// <inheritdoc />
        private protected override T ProcessResult<T>(ParseResult<T> result, string type)
        {
            this.iterator.Skip(result.Length);
            return base.ProcessResult(result, type);
        }

        /// <inheritdoc />
        private protected override ReadOnlySpan<char> ReadTrimmedString()
        {
            this.ReadStringIntoBuffer();
            this.stringBuffer.TrimEnds(IsWhiteSpace);
            return this.stringBuffer.CreateSpan();
        }

        private FormatException CreateUnexpectedTokenException()
        {
            return new FormatException(
                $"Unexpected token starting at {this.GetCurrentPosition()}.");
        }

        private void Expect(char c)
        {
            if (this.iterator.Current != c)
            {
                this.startPosition = this.iterator.Position;
                throw new FormatException($"Expected a '{c}' at {this.GetCurrentPosition()}.");
            }

            this.iterator.MoveNext();
        }

        private void Expect(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                this.iterator.MoveNext();
                if (this.iterator.Current != str[i])
                {
                    this.startPosition = this.iterator.Position;
                    throw this.CreateUnexpectedTokenException();
                }
            }

            // Move past the token
            this.iterator.MoveNext();
        }

        private void ReadStringIntoBuffer()
        {
            this.SkipWhiteSpace();

            this.Expect('"');
            this.stringBuffer.Clear();
            do
            {
                if (this.iterator.Current == '"')
                {
                    this.iterator.MoveNext();
                    return;
                }

                this.stringBuffer.Append(JsonStringEncoding.DecodeChar(this.iterator));
            }
            while (this.iterator.MoveNext());

            // We reached the end of the stream without finding a closing
            // quotation mark :(
            throw new FormatException(
                $"Missing closing double quotes for string starting at {this.GetCurrentPosition()}.");
        }

        private void SkipWhiteSpace()
        {
            while (IsWhiteSpace(this.iterator.Current))
            {
                // We rely on the fact that when the iterator moves to the end
                // it clears the Current property, so the above check will fail
                this.iterator.MoveNext();
            }

            this.startPosition = this.iterator.Position;
        }
    }
}
