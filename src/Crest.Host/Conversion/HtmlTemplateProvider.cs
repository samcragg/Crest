// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using Crest.Abstractions;
    using Crest.Host.Engine;

    /// <summary>
    /// Provides the default template for HTML pages.
    /// </summary>
    [OverridableService]
    internal sealed class HtmlTemplateProvider : IHtmlTemplateProvider
    {
        private const string Hint = "<p>You are seeing this page because you either requested HTML or no <code>Accept</code> header was specified. To return the object in another format, specify the <code>Accept</code> header with its MIME media type (for example, to return a JSON representation of the object, specify <code>Accept: application/json</code>).</p>";

        private const string Html = "<!doctype html>\n" +
"<html><head>\n" +
"<meta charset=utf-8>\n" +
"<title>API Service</title>\n" +
"<style>\n" +
"html{margin:0;padding:0}\n" +
"body{font-family:Arial,Helvetica,sans-serif;margin:10px auto;max-width:960px}\n" +
"h1{color:#8c1;font-size:36px;line-height:40px;margin:20px 0 5px}\n" +
"p{color:#555;font-size:18px;line-height:20px;margin:10px 0 5px}\n" +
"th{text-align: right}\n" +
"footer{margin-top:50px;text-align:center}\n" +
"</style>\n" +
"</head><body>\n" +
"<footer><p>\n" +
"<svg height='50px' style='vertical-align:middle' viewBox='0 0 100 100'><path d='m21.8 22.8v17.5c0 37.4 25.5 46.8 28.2 47.7 2.7-0.892 28.2-10.3 28.2-47.7v-17.5c-1.9-1.7-12.9-10.8-28.2-10.8s-26.3 9.11-28.2 10.8z' stroke='#000' stroke-width='2' fill='none'/><path d='m22.7 52.7c30.6-18.2 52.2 9.86 52.2 9.86' stroke='#000' stroke-width='1px' fill='none'/><path d='m77.5 50.3c-16.8-16-28.3-3.3-28.3-3.3' stroke='#000' stroke-width='1px' fill='none'/><path d='m38.2 20.1c-3 0-5.56 1.71-6.86 4.19-3.11 0.303-5.54 2.95-5.54 6.14 0 3.42 2.78 6.2 6.2 6.2h13.4c2.84 0 5.15-2.31 5.15-5.18 0-2.73-2.12-4.96-4.79-5.12-0.744-3.53-3.83-6.23-7.6-6.23z' stroke='#000' fill='none'/></svg>\n" +
"<small>Service powered by <a href='https://github.com/samcragg/Crest'>Crest</a>.</small>\n" +
"</p></footer>\n" +
"</body></html>";

        /// <inheritdoc />
        public int ContentLocation => 403;  // Just after the <body> tag

        /// <inheritdoc />
        public string HintText => Hint;

        /// <inheritdoc />
        public string Template => Html;
    }
}
