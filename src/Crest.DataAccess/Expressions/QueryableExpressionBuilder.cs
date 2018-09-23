// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Crest.Core.Logging;
    using Crest.DataAccess.Parsing;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Allows the building of expressions for <see cref="IQueryable"/>.
    /// </summary>
    internal sealed partial class QueryableExpressionBuilder
    {
        private static readonly ILog Logger = Log.For<QueryableExpressionBuilder>();
        private static readonly BuiltInMethods Methods = new BuiltInMethods();
        private readonly MappingCache mappings;
        private readonly CsvSplitter splitter;
        private bool hasPreviousSort;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryableExpressionBuilder"/> class.
        /// </summary>
        /// <param name="mappings">Contains the mappings between properties.</param>
        public QueryableExpressionBuilder(MappingCache mappings)
        {
            this.mappings = mappings;
            this.splitter = new CsvSplitter();
        }

        /// <summary>
        /// Creates a filtered query.
        /// </summary>
        /// <param name="query">The existing query to filter.</param>
        /// <param name="property">The property to filter on.</param>
        /// <param name="method">The type of the filter.</param>
        /// <param name="value">The parameter for the filter method.</param>
        /// <returns>A query that is filtered to the specified information.</returns>
        public IQueryable CreateFilter(IQueryable query, PropertyInfo property, FilterMethod method, string value)
        {
            Func<Expression, Expression> transformer = x => this.BuildFilterExpression(method, x, value);
            Expression lambda = this.BuildLambda(query.ElementType, property, transformer);
            return query.Provider.CreateQuery(Expression.Call(
                null,
                Methods.Where.MakeGenericMethod(query.ElementType),
                new Expression[] { query.Expression, lambda }));
        }

        /// <summary>
        /// Creates a sorted query.
        /// </summary>
        /// <param name="query">The existing query to sort.</param>
        /// <param name="property">The property to sort by.</param>
        /// <param name="direction">The sort direction.</param>
        /// <returns>A query that is ordered by the specified information.</returns>
        public IQueryable CreateSort(IQueryable query, PropertyInfo property, SortDirection direction)
        {
            LambdaExpression lambda = this.BuildLambda(query.ElementType, property, x => x);
            MethodInfo sortMethod = this.GetSortMethod(direction);
            return query.Provider.CreateQuery(Expression.Call(
                null,
                sortMethod.MakeGenericMethod(query.ElementType, lambda.Body.Type),
                new Expression[] { query.Expression, lambda }));
        }

        private static Expression BuildArithmeticFilterExpression(FilterMethod method, Expression input, string value)
        {
            Expression constant = GetConstant(input, value);
            switch (method)
            {
                case FilterMethod.GreaterThan:
                    return Expression.GreaterThan(input, constant);

                case FilterMethod.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(input, constant);

                case FilterMethod.LessThan:
                    return Expression.LessThan(input, constant);

                default:
                    Assert(method == FilterMethod.LessThanOrEqual, "Unexpected filter method " + method);
                    return Expression.LessThanOrEqual(input, constant);
            }
        }

        private static Expression BuildStringFilterExpression(FilterMethod method, Expression input, string value)
        {
            Expression constant = Expression.Constant(value);
            switch (method)
            {
                case FilterMethod.Contains:
                    return Expression.Call(input, Methods.StringContains, constant);

                case FilterMethod.EndsWith:
                    return Expression.Call(input, Methods.EndsWith, constant);

                default:
                    Assert(method == FilterMethod.StartsWith, "Unexpected filter method " + method);
                    return Expression.Call(input, Methods.StartsWith, constant);
            }
        }

        private static Expression GetConstant(Expression input, string value)
        {
            try
            {
                return Expression.Constant(
                    Convert.ChangeType(value, input.Type, CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Logger.WarnException(
                    "Unable to change '{value}' into a '{type}'",
                    ex,
                    value,
                    input.Type);
                throw new InvalidOperationException($"Value was in the incorrect format: '{value}'");
            }
        }

        private Expression BuildFilterExpression(FilterMethod method, Expression input, string value)
        {
            if ((int)method < 0x10)
            {
                return this.BuildGeneralFilterExpression(method, input, value);
            }
            else if ((int)method < 0x20)
            {
                return BuildArithmeticFilterExpression(method, input, value);
            }
            else
            {
                return BuildStringFilterExpression(method, input, value);
            }
        }

        private Expression BuildGeneralFilterExpression(FilterMethod method, Expression input, string value)
        {
            switch (method)
            {
                case FilterMethod.Equals:
                    return Expression.Equal(input, GetConstant(input, value));

                case FilterMethod.In:
                    var sourceList = new HashSet<string>(this.splitter.Split(value), StringComparer.Ordinal);
                    return Expression.Call(
                        Methods.EnumerableContains,
                        Expression.Constant(sourceList),
                        input);

                default:
                    Assert(method == FilterMethod.NotEquals, "Unexpected filter method " + method);
                    return Expression.NotEqual(input, GetConstant(input, value));
            }
        }

        private LambdaExpression BuildLambda(Type sourceType, PropertyInfo property, Func<Expression, Expression> transformer)
        {
            if (property.DeclaringType == sourceType)
            {
                ParameterExpression parameter = Expression.Parameter(sourceType);
                return Expression.Lambda(
                    transformer(Expression.Property(parameter, property)),
                    parameter);
            }
            else
            {
                return this.mappings.CreateMemberAccess(sourceType, property, transformer);
            }
        }

        private MethodInfo GetSortMethod(SortDirection direction)
        {
            if (this.hasPreviousSort)
            {
                return direction == SortDirection.Ascending ?
                    Methods.ThenBy :
                    Methods.ThenByDescending;
            }
            else
            {
                this.hasPreviousSort = true;
                return direction == SortDirection.Ascending ?
                    Methods.OrderBy :
                    Methods.OrderByDescending;
            }
        }
    }
}
