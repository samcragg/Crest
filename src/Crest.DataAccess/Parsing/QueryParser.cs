// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Core.Logging;

    /// <summary>
    /// Parses the query information.
    /// </summary>
    internal sealed class QueryParser
    {
        private const string SortParameter = "order";
        private static readonly ILog Logger = Log.For<QueryParser>();

        private static readonly Dictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> TypeProperties =
            new Dictionary<Type, IReadOnlyDictionary<string, PropertyInfo>>();

        private readonly DataSource query;
        private readonly CsvSplitter splitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParser"/> class.
        /// </summary>
        /// <param name="query">Contains the query information.</param>
        public QueryParser(DataSource query)
        {
            this.query = query;
            this.splitter = new CsvSplitter();
        }

        /// <summary>
        /// Extracts the filters from the query.
        /// </summary>
        /// <param name="type">The type to filter.</param>
        /// <returns>A sequence of filters to apply.</returns>
        public IEnumerable<FilterInfo> GetFilters(Type type)
        {
            IReadOnlyDictionary<string, PropertyInfo> properties = GetPropertiesFor(type);
            foreach (string member in this.query.Members)
            {
                if (properties.TryGetValue(member, out PropertyInfo property))
                {
                    IEnumerable<string> values = this.GetValuesForKey(member);
                    if (TryParseFilter(values, out FilterMethod method, out string value))
                    {
                        yield return new FilterInfo(property, method, value);
                    }
                    else
                    {
                        Logger.WarnFormat("Unable to parse filter '{filter}'", values.FirstOrDefault());
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the sorting from the query.
        /// </summary>
        /// <param name="type">The type to filter.</param>
        /// <returns>A sequence of sorting to apply.</returns>
        public IEnumerable<(PropertyInfo property, SortDirection direction)> GetSorting(Type type)
        {
            IReadOnlyDictionary<string, PropertyInfo> properties = GetPropertiesFor(type);
            foreach (string sort in this.GetValuesForKey(this.FindSortMember()))
            {
                foreach (string value in this.splitter.Split(sort))
                {
                    if (TryParseSort(value, out SortDirection direction, out string propertyName))
                    {
                        if (properties.TryGetValue(propertyName, out PropertyInfo property))
                        {
                            yield return (property, direction);
                        }
                        else
                        {
                            Logger.WarnFormat("Unable to find sort property '{property}'", propertyName);
                        }
                    }
                    else
                    {
                        Logger.WarnFormat("Unable to parse sort query '{sort}'", value);
                    }
                }
            }
        }

        private static IReadOnlyDictionary<string, PropertyInfo> GetPropertiesFor(Type type)
        {
            const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
            bool HasGetterAndSetter(PropertyInfo property)
            {
                return (property.GetMethod != null) && (property.SetMethod != null);
            }

            lock (TypeProperties)
            {
                if (!TypeProperties.TryGetValue(type, out IReadOnlyDictionary<string, PropertyInfo> properties))
                {
                    properties =
                        type.GetProperties(PublicInstance)
                            .Where(HasGetterAndSetter)
                            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

                    TypeProperties.Add(type, properties);
                }

                return properties;
            }
        }

        private static bool MatchEnum<T>(string value, int colon, string expected, T valueIfMatch, ref T result)
        {
            if ((expected.Length == colon) &&
                string.Compare(value, 0, expected, 0, colon, StringComparison.OrdinalIgnoreCase) == 0)
            {
                result = valueIfMatch;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ParseMethod(string filter, int colon, out FilterMethod method)
        {
            method = default;
            if (colon == 2)
            {
                return
                    MatchEnum(filter, colon, "eq", FilterMethod.Equals, ref method) ||
                    MatchEnum(filter, colon, "ne", FilterMethod.NotEquals, ref method) ||
                    MatchEnum(filter, colon, "in", FilterMethod.In, ref method) ||
                    MatchEnum(filter, colon, "gt", FilterMethod.GreaterThan, ref method) ||
                    MatchEnum(filter, colon, "ge", FilterMethod.GreaterThanOrEqual, ref method) ||
                    MatchEnum(filter, colon, "lt", FilterMethod.LessThan, ref method) ||
                    MatchEnum(filter, colon, "le", FilterMethod.LessThanOrEqual, ref method);
            }
            else
            {
                return
                    MatchEnum(filter, colon, "contains", FilterMethod.Contains, ref method) ||
                    MatchEnum(filter, colon, "endswith", FilterMethod.EndsWith, ref method) ||
                    MatchEnum(filter, colon, "startswith", FilterMethod.StartsWith, ref method);
            }
        }

        private static bool TryParseFilter(IEnumerable<string> query, out FilterMethod method, out string value)
        {
            string filter = query.First();
            int colon = filter.IndexOf(':');
            if (colon < 0)
            {
                method = FilterMethod.Equals;
                value = filter;
                return true;
            }

            if (ParseMethod(filter, colon, out method))
            {
                value = filter.Substring(colon + 1);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        private static bool TryParseSort(string value, out SortDirection direction, out string property)
        {
            int colon = value.IndexOf(':');
            if (colon < 0)
            {
                direction = SortDirection.Ascending;
                property = value;
                return true;
            }

            direction = default;
            if (MatchEnum(value, colon, "asc", SortDirection.Ascending, ref direction) ||
                MatchEnum(value, colon, "desc", SortDirection.Descending, ref direction))
            {
                property = value.Substring(colon + 1);
                return true;
            }
            else
            {
                property = default;
                return false;
            }
        }

        private string FindSortMember()
        {
            return this.query.Members.FirstOrDefault(m =>
                string.Equals(SortParameter, m, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<string> GetValuesForKey(string key)
        {
            IEnumerable<string> values = null;
            if (key != null)
            {
                values = (dynamic)this.query.GetValue(key);
            }

            return values ?? Enumerable.Empty<string>();
        }
    }
}
