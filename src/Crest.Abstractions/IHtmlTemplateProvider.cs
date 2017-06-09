// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    /// <summary>
    /// Allows the specification of the HTML page template when returning HTML
    /// pages from the service.
    /// </summary>
    public interface IHtmlTemplateProvider
    {
        /// <summary>
        /// Gets the index to insert the generated content into the value
        /// returned by <see cref="Template"/>.
        /// </summary>
        int ContentLocation { get; }

        /// <summary>
        /// Gets the text to display when returning an HTML page for a response
        /// that indicates why HTML is being returned instead of another format.
        /// </summary>
        string HintText { get; }

        /// <summary>
        /// Gets the HTML page template to return.
        /// </summary>
        string Template { get; }
    }
}
