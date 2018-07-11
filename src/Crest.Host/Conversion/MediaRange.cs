// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;

    /// <summary>
    /// Allows the matching of media types.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc7231#section-5.3.2"/>
    internal sealed class MediaRange
    {
        private const int AnyMatch = -1;
        private const string InvalidMediaType = "Invalid media type format.";
        private const string InvalidQualityValue = "Invalid quality value.";
        private readonly string originalString;
        private readonly int subTypeEnd;
        private readonly int subTypeStart;
        private readonly int typeEnd;
        private readonly int typeStart;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaRange"/> class.
        /// </summary>
        /// <param name="range">The substring to parse.</param>
        public MediaRange(StringSegment range)
        {
            // https://tools.ietf.org/html/rfc7231#section-5.3.2
            // "*/*" / (type "/" "*") / (type "/" subtype)
            int index = SkipWhitespace(range.String, range.Start, range.End);
            this.originalString = range.String;

            this.typeStart = index;
            index = SkipToken(this.originalString, index, range.End);
            this.typeEnd = index;
            if ((index == range.End) || (this.originalString[index] != '/'))
            {
                throw new ArgumentException(InvalidMediaType);
            }

            index++; // Skip the separator
            this.subTypeStart = index;
            index = SkipToken(this.originalString, index, range.End);
            this.subTypeEnd = index;
            if (this.subTypeEnd == this.subTypeStart)
            {
                throw new ArgumentException(InvalidMediaType);
            }

            // Squash the any matches ('*') so that we can quickly check later
            // if we need to match that part or not
            this.SquashAnyTypes(ref this.typeStart, this.typeEnd);
            this.SquashAnyTypes(ref this.subTypeStart, this.subTypeEnd);

            this.Quality = FindQuality(this.originalString, index, range.End);
        }

        /// <summary>
        /// Gets the scaled quality.
        /// </summary>
        /// <remarks>
        /// The quality is accurate up to three digits, hence the values have
        /// been scaled by 1000 to allow integers to be used.
        /// </remarks>
        public int Quality { get; }

        /// <summary>
        /// Determines whether the media range specified by this instance
        /// matches the media type of the specified value.
        /// </summary>
        /// <param name="other">The media range to compare with.</param>
        /// <returns>
        /// <c>true</c> if the media types match; otherwise, <c>false</c>.
        /// </returns>
        internal bool MediaTypesMatch(MediaRange other)
        {
            if ((this.typeStart != AnyMatch) &&
                (other.typeStart != AnyMatch) &&
                !this.ComparePart(
                    this.typeStart,
                    this.typeEnd,
                    other,
                    other.typeStart,
                    other.typeEnd))
            {
                return false;
            }

            if ((this.subTypeStart != AnyMatch) &&
                (other.subTypeStart != AnyMatch) &&
                !this.ComparePart(
                    this.subTypeStart,
                    this.subTypeEnd,
                    other,
                    other.subTypeStart,
                    other.subTypeEnd))
            {
                return false;
            }

            return true;
        }

        private static int FindQuality(string value, int start, int end)
        {
            int index = SkipWhitespace(value, start, end);
            while (index < (end - 1))
            {
                if (value[index] != ';')
                {
                    break;
                }

                index++; // Skip the ';'
                index = SkipWhitespace(value, index, end);
                if (ParseQualityParameter(value, index, end, out int quality))
                {
                    return quality;
                }

                index = value.IndexOf(';', index, end - index);
                if (index < 0)
                {
                    break;
                }
            }

            // If the quality isn't specified it defaults to 1.0
            return 1000;
        }

        private static bool IsTChar(char c)
        {
            // https://tools.ietf.org/html/rfc7230#section-3.2.6
            // token = 1*tchar
            // tchar = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" /
            //         "." / "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA
            //
            // https://tools.ietf.org/html/rfc5234#appendix-B.1
            // ALPHA =  %x41-5A / %x61-7A    ; A-Z / a-z
            // DIGIT =  %x30-39              ; 0-9
            //
            // ----------
            // | 21 | ! |
            // | 23 | # |
            // | 24 | $ |
            // | 25 | % |
            // | 26 | & |
            // | 27 | ' |
            // | 2A | * |
            // | 2B | + |
            // | 2D | - |
            // | 2E | . |
            // | 30 | 0 |
            //     ...
            // | 39 | 9 |
            // | 41 | A |
            //     ...
            // | 5A | Z |
            // | 5E | ^ |
            // | 5F | _ |
            // | 60 | ` |
            // | 61 | a |
            //     ...
            // | 7A | z |
            // | 7C | | |
            // | 7E | ~ |
            // ----------
            if (c < '\x21')
            {
                return false;
            }
            else if (c <= '\x27')
            {
                return c != '\x22';
            }
            else if (c <= '\x2E')
            {
                return (c >= '\x2A') && (c != '\x2C');
            }
            else if (c <= '\x39')
            {
                return c >= '\x30';
            }
            else if (c <= '\x5A')
            {
                return c >= '\x41';
            }
            else if (c <= '\x7A')
            {
                return c >= '\x5E';
            }
            else
            {
                return (c == '\x7C') || (c == '\x7E');
            }
        }

        private static bool ParseQualityParameter(string value, int start, int end, out int quality)
        {
            // Smallest value is three characters, e.g. 'q=1'
            if ((start <= end - 3) &&
                ((value[start] == 'q') || (value[start] == 'Q')) &&
                (value[start + 1] == '='))
            {
                quality = ParseQualityValue(value, start + 2, end);
                return true;
            }

            quality = 0;
            return false;
        }

        private static int ParseQualityValue(string value, int index, int end)
        {
            // https://tools.ietf.org/html/rfc7231#section-5.3.1
            // qvalue = ( "0" [ "." 0*3DIGIT ] )
            //        / ( "1" [ "." 0*3("0") ] )
            //
            // However, we'll be generous and not care about remaining digits
            // and cap values at 1 (i.e. 1.999 would be 1000)
            char c = value[index];
            if (c == '1')
            {
                return 1000;
            }
            else if (c != '0')
            {
                throw new ArgumentException(InvalidQualityValue);
            }

            int result = 0;
            index++;
            if (index < end)
            {
                c = value[index];
                if ((c == '.') &&
                    TryAddDigit(value, index + 1, end, 100, ref result) &&
                    TryAddDigit(value, index + 2, end, 10, ref result))
                {
                    TryAddDigit(value, index + 3, end, 1, ref result);
                }
            }

            return result;
        }

        private static int SkipToken(string value, int index, int end)
        {
            while (index < end)
            {
                if (!IsTChar(value[index]))
                {
                    break;
                }

                index++;
            }

            return index;
        }

        private static int SkipWhitespace(string value, int index, int end)
        {
            while (index != end)
            {
                if (!char.IsWhiteSpace(value[index]))
                {
                    break;
                }

                index++;
            }

            return index;
        }

        private static bool TryAddDigit(string value, int index, int end, int multiplier, ref int quality)
        {
            if (index < end)
            {
                char c = value[index];
                uint digit = (uint)(c - '0');
                if (digit < 10)
                {
                    quality += (int)digit * multiplier;
                    return true;
                }
            }

            return false;
        }

        private bool ComparePart(int start, int end, MediaRange other, int otherStart, int otherEnd)
        {
            int length = end - start;
            int otherLength = otherEnd - otherStart;
            if (length != otherLength)
            {
                return false;
            }

            int compare = string.Compare(
                this.originalString,
                start,
                other.originalString,
                otherStart,
                length,
                StringComparison.OrdinalIgnoreCase);

            return compare == 0;
        }

        private void SquashAnyTypes(ref int index, int end)
        {
            if (((index + 1) == end) && (this.originalString[index] == '*'))
            {
                index = AnyMatch;
            }
        }
    }
}
