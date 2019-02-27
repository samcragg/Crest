// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Util
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Crest.Core;

    /// <summary>
    /// Builds generic expressions for a link to a method that can have the
    /// arguments injected in.
    /// </summary>
    internal sealed partial class LinkExpressionBuilder
    {
        private static readonly MethodInfo AppendDynamicQueryMethod =
            typeof(LinkExpressionBuilder).GetMethod(nameof(AppendDynamicQuery), BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly MethodInfo StringBufferAppend =
            typeof(StringBuffer).GetMethod(nameof(StringBuffer.Append), new[] { typeof(string) });

        private static readonly MethodInfo StringBufferAppendChar =
            typeof(StringBuffer).GetMethod(nameof(StringBuffer.Append), new[] { typeof(char) });

        private static readonly MethodInfo StringBufferDispose =
            typeof(StringBuffer).GetMethod(nameof(StringBuffer.Dispose));

        private static readonly MethodInfo StringBufferToString =
            typeof(StringBuffer).GetMethod(nameof(StringBuffer.ToString));

        /// <summary>
        /// Creates a delegate that converts the route on the specified method
        /// to a link.
        /// </summary>
        /// <typeparam name="T">The type of the delegate to return.</typeparam>
        /// <param name="method">The method containing the route.</param>
        /// <returns>A delegate that can be used to generate a link.</returns>
        public T FromMethod<T>(MethodInfo method)
            where T : Delegate
        {
            (string route, int version) = GetRouteInformation(method);

            var builder = new DelegateBuilder(version);
            var parser = new RouteParser(builder, method.GetParameters());
            parser.Parse(route);
            return builder.Build<T>().Compile();
        }

        private static void AppendDynamicQuery(StringBuffer buffer, object value)
        {
            bool writeSeparator = false;
            foreach (PropertyInfo property in value.GetType().GetProperties())
            {
                if (writeSeparator)
                {
                    buffer.Append('&');
                }

                string propertyValue = Convert.ToString(property.GetValue(value), CultureInfo.InvariantCulture);
                buffer.Append(property.Name);
                buffer.Append('=');
                UrlValueConverter.AppendEscapedString(buffer, propertyValue);
                writeSeparator = true;
            }
        }

        private static (string route, int version) GetRouteInformation(MethodInfo method)
        {
            RouteAttribute route = method.GetCustomAttributes<RouteAttribute>().FirstOrDefault();
            VersionAttribute version = method.GetCustomAttribute<VersionAttribute>();
            if ((route == null) || (version == null))
            {
                throw new InvalidOperationException(
                    "Unable to find route information on '" + method.Name + "'");
            }

            string routeUrl = route.Route;
            if (routeUrl.FirstOrDefault() != '/')
            {
                routeUrl = "/" + routeUrl;
            }

            return (routeUrl, version.From);
        }
    }
}
