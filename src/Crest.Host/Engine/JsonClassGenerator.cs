// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Crest.Core.Logging;
    using Crest.Host.IO;

    /// <summary>
    /// Generates code to change the properties of a class based on JSON data.
    /// </summary>
    internal class JsonClassGenerator
    {
        private static readonly ILog Logger = Log.For<JsonClassGenerator>();

        /// <summary>
        /// Creates an action that copies the specified JSON data to the
        /// relevant properties in the type.
        /// </summary>
        /// <param name="type">
        /// The type of the object that will be passed in to the returned action.
        /// </param>
        /// <param name="json">The JSON data.</param>
        /// <returns>
        /// An action that will set the properties to the matching data.
        /// </returns>
        public virtual Action<object> CreatePopulateMethod(Type type, string json)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(object));

            // We get passed an object so we need to cast it to the correct
            // type to set its properties
            ParameterExpression instance = Expression.Variable(type);
            var body = new List<Expression>
            {
                Expression.Assign(instance, Expression.Convert(parameter, type)),
            };

            var parser = new JsonObjectParser(json);
            body.AddRange(AssignProperties(instance, parser.GetPairs()));

            return Expression.Lambda<Action<object>>(
                Expression.Block(new[] { instance }, body),
                parameter).Compile();
        }

        private static IEnumerable<Expression> AssignProperties(
            ParameterExpression instance,
            IEnumerable<KeyValuePair<string, string>> pairs)
        {
            IReadOnlyDictionary<string, PropertyInfo> properties =
                instance.Type
                    .GetProperties()
                    .Where(p => p.CanWrite)
                    .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string> pair in pairs)
            {
                if (properties.TryGetValue(pair.Key, out PropertyInfo property))
                {
                    yield return CreateAssignment(instance, property, pair.Value);
                }
                else
                {
                    Logger.ErrorFormat(
                        "Unable to find property on {type} for {key}",
                        instance.Type.Name,
                        pair.Key);
                }
            }
        }

        private static Expression ConvertValue(string value, Type type)
        {
            if (value != null)
            {
                try
                {
                    object converted = Convert.ChangeType(
                        value,
                        Nullable.GetUnderlyingType(type) ?? type,
                        CultureInfo.InvariantCulture);

                    return Expression.Constant(converted, type);
                }
                catch
                {
                    Logger.ErrorFormat(
                        "Unable to convert '{value}' to {type}",
                        value,
                        type.Name);
                }
            }

            return Expression.Default(type);
        }

        private static Expression CreateAssignment(Expression instance, PropertyInfo property, string value)
        {
            Type propertyType = property.PropertyType;
            Expression createValue = null;
            if (propertyType.IsArray)
            {
                var parser = new JsonObjectParser(value);
                Type elementType = propertyType.GetElementType();

                createValue = Expression.NewArrayInit(
                    elementType,
                    parser.GetArrayValues().Select(s => ConvertValue(s, elementType)));
            }
            else
            {
                createValue = ConvertValue(value, propertyType);
            }

            return Expression.Assign(
                Expression.Property(instance, property),
                createValue);
        }
    }
}
