// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Abstractions;
    using Crest.Host.Engine;

    /// <summary>
    /// Creates a <see cref="IContentConverter"/> based on the request information.
    /// </summary>
    [OverridableService]
    internal sealed class ContentConverterFactory : IContentConverterFactory
    {
        private const string DefaultAcceptType = @"application/json";
        private readonly IContentConverter[] converters;
        private readonly MediaRange[] ranges;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentConverterFactory"/> class.
        /// </summary>
        /// <param name="converters">The available converters.</param>
        public ContentConverterFactory(IEnumerable<IContentConverter> converters)
        {
            var ordered =
                (from converter in converters
                 from format in converter.Formats
                 orderby converter.Priority descending
                 select new { converter, format })
                .ToList();

            this.converters = new IContentConverter[ordered.Count];
            this.ranges = new MediaRange[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
            {
                this.converters[i] = ordered[i].converter;
                this.ranges[i] = new MediaRange(new StringSegment(ordered[i].format));
            }
        }

        /// <inheritdoc />
        public IContentConverter GetConverter(string accept)
        {
            if (string.IsNullOrWhiteSpace(accept))
            {
                accept = DefaultAcceptType;
            }

            List<MediaRange> ranges = ParseRanges(accept);
            ranges.Sort((a, b) => b.Quality.CompareTo(a.Quality)); // Reverse sort

            foreach (MediaRange range in ranges)
            {
                IContentConverter converter = this.FindConverter(range);
                if (converter != null)
                {
                    return converter;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public void PrimeConverters(Type type)
        {
            Check.IsNotNull(type, nameof(type));

            System.Diagnostics.Trace.TraceInformation("Generating serializers for '{0}'", type.FullName);
            foreach (IContentConverter converter in this.converters)
            {
                converter.Prime(type);
            }
        }

        private static List<MediaRange> ParseRanges(string accept)
        {
            var ranges = new List<MediaRange>();

            int start = 0;
            int end = accept.IndexOf(',') + 1;
            while (end > 0)
            {
                ranges.Add(new MediaRange(new StringSegment(accept, start, end)));
                start = end;
                end = accept.IndexOf(',', start) + 1;
            }

            ranges.Add(new MediaRange(new StringSegment(accept, start, accept.Length)));
            return ranges;
        }

        private IContentConverter FindConverter(MediaRange accept)
        {
            IContentConverter bestConverter = null;
            int bestQuality = 0;
            for (int i = 0; i < this.ranges.Length; i++)
            {
                MediaRange range = this.ranges[i];
                if (range.MediaTypesMatch(accept) && (range.Quality > bestQuality))
                {
                    bestQuality = range.Quality;
                    bestConverter = this.converters[i];
                }
            }

            return bestConverter;
        }
    }
}
