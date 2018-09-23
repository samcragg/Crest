// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using static System.Diagnostics.Debug;

    /// <content>
    /// Contains the nested <see cref="BuiltInMethods"/> class.
    /// </content>
    internal sealed partial class QueryableExpressionBuilder
    {
        private class BuiltInMethods
        {
            private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

            public BuiltInMethods()
            {
                Type[] singleString = new[] { typeof(string) };
                this.EndsWith = typeof(string).GetMethod(nameof(string.EndsWith), singleString);
                this.StartsWith = typeof(string).GetMethod(nameof(string.StartsWith), singleString);
                this.StringContains = typeof(string).GetMethod(nameof(string.Contains), singleString);

                this.EnumerableContains = GetEnumerableMethod<string, bool>(Enumerable.Contains);
                this.OrderByDescending = GetQueryableMethod(nameof(Queryable.OrderByDescending));
                this.OrderBy = GetQueryableMethod(nameof(Queryable.OrderBy));
                this.ThenByDescending = GetQueryableMethod(nameof(Queryable.ThenByDescending));
                this.ThenBy = GetQueryableMethod(nameof(Queryable.ThenBy));
                this.Where = GetQueryableMethod(nameof(Queryable.Where));
            }

            public MethodInfo EndsWith { get; }

            public MethodInfo EnumerableContains { get; }

            public MethodInfo OrderBy { get; }

            public MethodInfo OrderByDescending { get; }

            public MethodInfo StartsWith { get; }

            public MethodInfo StringContains { get; }

            public MethodInfo ThenBy { get; }

            public MethodInfo ThenByDescending { get; }

            public MethodInfo Where { get; }

            private static MethodInfo GetEnumerableMethod<T, TResult>(Func<IEnumerable<T>, T, TResult> func)
            {
                return func.Method;
            }

            private static MethodInfo GetQueryableMethod(string name)
            {
                bool IsExpressionWithTwoFunctionArgument(Type type)
                {
                    Assert(
                        type.IsGenericType && (type.GenericTypeArguments.Length == 1),
                        "Expected the parameter to take the generic type.");

                    Type functionType = type.GenericTypeArguments[0];
                    return functionType.Name == "Func`2";
                }

                return (from member in typeof(Queryable).GetMember(name, MemberTypes.Method, PublicStatic)
                        let method = (MethodInfo)member
                        let parameters = method.GetParameters()
                        where (parameters.Length == 2) && IsExpressionWithTwoFunctionArgument(parameters[1].ParameterType)
                        select method).FirstOrDefault();
            }
        }
    }
}
