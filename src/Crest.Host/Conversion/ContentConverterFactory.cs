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
    using Crest.Core.Logging;
    using Crest.Core.Util;
    using Crest.Host.Engine;

    /// <summary>
    /// Creates a <see cref="IContentConverter"/> based on the request information.
    /// </summary>
    [OverridableService]
    internal sealed class ContentConverterFactory : IContentConverterFactory
    {
        private const string DefaultAcceptType = @"application/json";
        private static readonly ILog Logger = Log.For<ContentConverterFactory>();
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
                string format = ordered[i].format;
                this.ranges[i] = new MediaRange(format, 0, format.Length);
            }
        }

        /// <inheritdoc />
        public IContentConverter GetConverterForAccept(string accept)
        {
            if (string.IsNullOrWhiteSpace(accept))
            {
                accept = DefaultAcceptType;
            }

            List<MediaRange> parsedRanges = ParseRanges(accept);
            parsedRanges.Sort((a, b) => b.Quality.CompareTo(a.Quality)); // Reverse sort

            foreach (MediaRange range in parsedRanges)
            {
                IContentConverter converter = this.FindConverterForAccept(range);
                if (converter != null)
                {
                    return converter;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public IContentConverter GetConverterFromContentType(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                var range = new MediaRange(content, 0, content.Length);
                for (int i = 0; i < this.converters.Length; i++)
                {
                    IContentConverter converter = this.converters[i];
                    if (converter.CanRead && this.ranges[i].MediaTypesMatch(range))
                    {
                        return converter;
                    }
                }
            }

            return null;
        }

        /// <inheritdoc />
        public void PrimeConverters(Type type)
        {
            Check.IsNotNull(type, nameof(type));

            Logger.InfoFormat("Generating serializers for '{type}'", type.FullName);
            foreach (IContentConverter converter in this.converters)
            {
                converter.Prime(type);
            }
        }

        private static List<MediaRange> ParseRanges(string accept)
        {
            var mediaRanges = new List<MediaRange>();

            int start = 0;
            int end = accept.IndexOf(',') + 1;
            while (end > 0)
            {
                mediaRanges.Add(new MediaRange(accept, start, end - start));
                start = end;
                end = accept.IndexOf(',', start) + 1;
            }

            mediaRanges.Add(new MediaRange(accept, start, accept.Length - start));
            return mediaRanges;
        }

        private IContentConverter FindConverterForAccept(MediaRange accept)
        {
            IContentConverter bestConverter = null;
            int bestQuality = 0;
            for (int i = 0; i < this.converters.Length; i++)
            {
                IContentConverter converter = this.converters[i];
                if (converter.CanWrite)
                {
                    MediaRange range = this.ranges[i];
                    if (range.MediaTypesMatch(accept) && (range.Quality > bestQuality))
                    {
                        bestQuality = range.Quality;
                        bestConverter = this.converters[i];
                    }
                }
            }

            return bestConverter;
        }
    }
}
