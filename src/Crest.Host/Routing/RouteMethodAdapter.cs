// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Invokes a method using the captured route data.
    /// </summary>
    internal sealed class RouteMethodAdapter
    {
        private readonly MethodInfo convertGenericTaskMethod =
            typeof(RouteMethodAdapter).GetTypeInfo().GetDeclaredMethod(nameof(ConvertGenericTask));

        private readonly MethodInfo convertTaskMethod =
            typeof(RouteMethodAdapter).GetTypeInfo().GetDeclaredMethod(nameof(ConvertTaskToNoContent));

        private readonly PropertyInfo dictionaryGetValue =
            typeof(IReadOnlyDictionary<string, object>).GetTypeInfo().GetDeclaredProperty("Item");

        private readonly MethodInfo dictionaryTryGetValue =
            typeof(IReadOnlyDictionary<string, object>).GetTypeInfo().GetDeclaredMethod(nameof(IReadOnlyDictionary<string, object>.TryGetValue));

        /// <summary>
        /// Creates an adapter for the specified method.
        /// </summary>
        /// <param name="factory">
        /// Used to create the instance to invoke the method on.
        /// </param>
        /// <param name="method">The method to invoke.</param>
        /// <returns>
        /// The adapter method that, when passed the captured parameters,
        /// returns a task returned by the method.
        /// </returns>
        public RouteMethod CreateMethod(Func<object> factory, MethodInfo method)
        {
            if (!typeof(Task).GetTypeInfo().IsAssignableFrom(method.ReturnType.GetTypeInfo()))
            {
                throw new ArgumentException("Method must return a task (" + method.Name + ")", nameof(method));
            }

            ParameterExpression captures = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), nameof(captures));
            ParameterExpression instance = Expression.Parameter(method.DeclaringType, nameof(instance));

            var locals = new List<ParameterExpression>();
            List<Expression> body = this.LoadParameters(locals, captures, method.GetParameters()).ToList();

            locals.Add(instance);
            body.Add(CreateInstance(instance, factory));
            body.Add(this.InvokeMethod(locals, instance, method));

            return Expression.Lambda<RouteMethod>(
                Expression.Block(locals, body),
                captures).Compile();
        }

        private static Task<object> ConvertGenericTask<T>(Task<T> value)
        {
            return value.ContinueWith<object>(
                t => t.Result,
                TaskContinuationOptions.ExecuteSynchronously);
        }

        private static Task<object> ConvertTaskToNoContent(Task value)
        {
            return value.ContinueWith<object>(
                t =>
                {
                    t.Wait(); // Force exceptions to be re-thrown
                    return NoContent.Value;
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        private static Expression CreateInstance(Expression instance, Func<object> factory)
        {
            // T instance = (T)factory();
            return Expression.Assign(
                instance,
                Expression.Convert(
                    Expression.Invoke(Expression.Constant(factory)),
                    instance.Type));
        }

        private static Expression GetDefaultValue(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
            {
                return Expression.Constant(parameter.DefaultValue);
            }
            else
            {
                return Expression.Default(parameter.ParameterType);
            }
        }

        private Expression GetCapturedValue(ref ParameterExpression temp, Expression captures, ParameterInfo parameter)
        {
            if (parameter.IsOptional)
            {
                // object temp;
                // if (captures.TryGetValue("parameter", out temp)) {
                //     (T)temp;
                // } else {
                //     DefaultValue;
                // }
                if (temp == null)
                {
                    temp = Expression.Parameter(typeof(object));
                }

                return Expression.Condition(
                    Expression.Call(
                        captures,
                        this.dictionaryTryGetValue,
                        Expression.Constant(parameter.Name),
                        temp),
                    Expression.Convert(temp, parameter.ParameterType),
                    GetDefaultValue(parameter));
            }
            else
            {
                // (T)captures["parameter"]
                return Expression.Convert(
                    Expression.Call(
                        captures,
                        this.dictionaryGetValue.GetMethod,
                        Expression.Constant(parameter.Name)),
                    parameter.ParameterType);
            }
        }

        private MethodInfo GetConvertReturnTypeMethod(Type returnType)
        {
            MethodInfo convertMethod;
            if (returnType == typeof(Task))
            {
                convertMethod = this.convertTaskMethod;
            }
            else
            {
                convertMethod = this.convertGenericTaskMethod.MakeGenericMethod(
                    returnType.GenericTypeArguments);
            }

            return convertMethod;
        }

        private Expression InvokeMethod(IReadOnlyList<Expression> locals, Expression instance, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Expression[] parameterExpressions = new Expression[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterExpressions[i] = locals[i];
            }

            Expression callMethod = Expression.Call(instance, method, parameterExpressions);
            MethodInfo convertMethod = this.GetConvertReturnTypeMethod(method.ReturnType);
            return Expression.Call(convertMethod, callMethod);
        }

        private IEnumerable<Expression> LoadParameters(IList<ParameterExpression> locals, Expression captures, ParameterInfo[] parameters)
        {
            ParameterExpression temp = null;
            foreach (ParameterInfo parameter in parameters)
            {
                // T parameter = GetCapturedValue(captures);
                ParameterExpression local = Expression.Parameter(parameter.ParameterType, parameter.Name);
                locals.Add(local);
                yield return Expression.Assign(
                    local,
                    this.GetCapturedValue(ref temp, captures, parameter));
            }

            if (temp != null)
            {
                locals.Add(temp);
            }
        }
    }
}
