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
    internal sealed class GenericCaptureNode : IMatchNode, IQueryValueConverter
    {
        private readonly TypeConverter converter;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericCaptureNode"/> class.
        /// </summary>
        /// <param name="parameter">
        /// The name of the parameter being captured.
        /// </param>
        /// <param name="type">
        /// The type of the property to convert to.
        /// </param>
        public GenericCaptureNode(string parameter, Type type)
        {
            this.converter = TypeDescriptor.GetConverter(type);
            this.ParameterName = parameter;
        }

        /// <inheritdoc />
        public string ParameterName { get; }

        /// <inheritdoc />
        public int Priority => 200;

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as GenericCaptureNode;
            if (node == null)
            {
                return false;
            }

            return string.Equals(this.ParameterName, node.ParameterName, StringComparison.Ordinal) &&
                   (this.converter.GetType() == node.converter.GetType());
        }

        /// <inheritdoc />
        public NodeMatchResult Match(StringSegment segment)
        {
            if (this.TryConvertValue(segment, out object value))
            {
                return new NodeMatchResult(this.ParameterName, value);
            }
            else
            {
                return NodeMatchResult.None;
            }
        }

        /// <inheritdoc />
        public bool TryConvertValue(StringSegment value, out object result)
        {
            if (this.converter.CanConvertFrom(typeof(string)))
            {
                // Just because it can convert from a string, doesn't mean the
                // string is in a valid format, hence Pokemon exception handling...
                try
                {
                    result = this.converter.ConvertFromInvariantString(value.ToString());
                    return true;
                }
                catch
                {
                    // Gotta catch 'em all
                }
            }

            result = null;
            return false;
        }
    }
}
