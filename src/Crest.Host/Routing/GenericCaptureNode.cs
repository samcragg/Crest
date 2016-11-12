// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Allows the capturing of information from the route and converting it
    /// with the types default TypeConverter.
    /// </summary>
    internal sealed class GenericCaptureNode : IMatchNode
    {
        private readonly TypeConverter converter;
        private readonly string property;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericCaptureNode"/> class.
        /// </summary>
        /// <param name="property">
        /// The name of the property to capture the value to.
        /// </param>
        /// <param name="type">
        /// The type of the property to convert to.
        /// </param>
        public GenericCaptureNode(string property, Type type)
        {
            this.converter = TypeDescriptor.GetConverter(type);
            this.property = property;
        }

        /// <inheritdoc />
        public int Priority
        {
            get { return 200; }
        }

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as GenericCaptureNode;
            if (node == null)
            {
                return false;
            }

            return string.Equals(this.property, node.property, StringComparison.Ordinal) &&
                   (this.converter.GetType() == node.converter.GetType());
        }

        /// <inheritdoc />
        public NodeMatchResult Match(StringSegment segment)
        {
            if (this.converter.CanConvertFrom(typeof(string)))
            {
                // Just because it can convert from a string, doesn't mean the
                // string is in a valid format, hence Pokemon exception handling...
                try
                {
                    object value = this.converter.ConvertFromInvariantString(segment.ToString());
                    return new NodeMatchResult(this.property, value);
                }
                catch
                {
                    // Gotta catch 'em all
                }
            }

            return NodeMatchResult.None;
        }
    }
}
