// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using Crest.Core.Logging;

    /// <summary>
    /// Allows the resolving of mappings between different types.
    /// </summary>
    internal class MappingCache
    {
        private static readonly ILog Logger = Log.For<MappingCache>();

        private readonly Dictionary<(Type from, Type to), Resolver> resolvers =
            new Dictionary<(Type, Type), Resolver>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingCache"/> class.
        /// </summary>
        /// <param name="mappingProviders">The source of the mapping information.</param>
        public MappingCache(IMappingProvider[] mappingProviders)
        {
            foreach (IMappingProvider provider in mappingProviders)
            {
                this.resolvers[(provider.Source, provider.Destination)] = CreateMappings(
                    provider.GenerateMappings(),
                    provider.Source,
                    provider.Destination);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingCache"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is only used to allow the type to be mocked in unit tests.
        /// </remarks>
        protected MappingCache()
        {
        }

        /// <summary>
        /// Creates an expression for accessing the member that is mapped to
        /// the specified property.
        /// </summary>
        /// <param name="targetType">The type to map to.</param>
        /// <param name="property">The property being accessed.</param>
        /// <param name="transformer">Used to modify the access expression.</param>
        /// <returns>
        /// An expression accessing a member that is used to assign values to
        /// the specified property.
        /// </returns>
        public virtual LambdaExpression CreateMemberAccess(
            Type targetType,
            PropertyInfo property,
            Func<Expression, Expression> transformer)
        {
            if (!this.resolvers.TryGetValue((property.DeclaringType, targetType), out Resolver resolver))
            {
                Logger.ErrorFormat(
                    "No mappings exist between {source} and {target}",
                    property.DeclaringType,
                    targetType);

                throw new InvalidOperationException("Unknown type");
            }

            if (!resolver.TryGetValue(property, out (ParameterExpression parameter, Expression expression) value))
            {
                Logger.ErrorFormat(
                    "No mapping found for {property} on {type}",
                    property,
                    property.PropertyType);

                throw new InvalidOperationException("Unknown parameter");
            }

            return Expression.Lambda(transformer(value.expression), value.parameter);
        }

        private static Resolver CreateMappings(Expression mappings, Type source, Type destination)
        {
            var resolver = new Resolver();
            var assignmentVisitor = new AssignmentVisitor(source, destination);
            var parameterVisitor = new ParameterVisitor();
            foreach (KeyValuePair<MemberInfo, Expression> assignment in assignmentVisitor.GetAssignments(mappings))
            {
                ParameterExpression parameter = parameterVisitor.FindParameter(assignment.Value, destination);
                if (parameter != null)
                {
                    resolver[assignment.Key] = (parameter, assignment.Value);
                }
            }

            return resolver;
        }

        private sealed class Resolver : Dictionary<MemberInfo, (ParameterExpression, Expression)>
        {
        }
    }
}
