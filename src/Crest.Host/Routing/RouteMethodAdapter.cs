// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;

    /// <summary>
    /// Invokes a method using the captured route data.
    /// </summary>
    internal sealed class RouteMethodAdapter
    {
        private readonly List<Expression> body = new List<Expression>();

        private readonly MethodInfo convertGenericTaskMethod =
            typeof(RouteMethodAdapter).GetMethod(nameof(ConvertGenericTask), BindingFlags.NonPublic | BindingFlags.Static);

        private readonly MethodInfo convertTaskMethod =
            typeof(RouteMethodAdapter).GetMethod(nameof(ConvertTaskToNoContent), BindingFlags.NonPublic | BindingFlags.Static);

        private readonly MethodInfo dictionaryGetValue =
            typeof(IReadOnlyDictionary<string, object>).GetProperty("Item").GetGetMethod();

        private readonly MethodInfo dictionaryTryGetValue =
            typeof(IReadOnlyDictionary<string, object>).GetMethod(nameof(IReadOnlyDictionary<string, object>.TryGetValue));

        private readonly MethodInfo serviceProviderGetservice =
            typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService));

        private readonly MethodInfo valueProviderGetValue =
            typeof(IValueProvider).GetProperty(nameof(IValueProvider.Value)).GetGetMethod();

        private ParameterExpression localValue;
        private ParameterExpression localValueProvider;

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
            this.body.Clear();
            this.AddLoadParameters(locals, captures, method.GetParameters());

            locals.Add(instance);
            this.body.Add(this.CreateInstance(captures, instance, factory));
            this.body.Add(this.InvokeMethod(locals, instance, method));

            return Expression.Lambda<RouteMethod>(
                Expression.Block(locals, this.body),
                captures).Compile();
        }

#pragma warning disable UseAsyncSuffix

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

#pragma warning restore UseAsyncSuffix

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

        private void AddLoadParameters(IList<ParameterExpression> locals, Expression captures, ParameterInfo[] parameters)
        {
            this.localValue = Expression.Variable(typeof(object));
            this.localValueProvider = Expression.Variable(typeof(IValueProvider));

            foreach (ParameterInfo parameter in parameters)
            {
                // T parameter = GetCapturedValue(captures);
                ParameterExpression local = Expression.Parameter(parameter.ParameterType, parameter.Name);
                locals.Add(local);
                this.AssignCapturedValue(local, captures, parameter);
            }

            // IMPORTANT: Add these at the end so that the locals are declared
            // in the same order as the parameters
            locals.Add(this.localValue);
            locals.Add(this.localValueProvider);
        }

        private void AssignCapturedValue(ParameterExpression parameterValue, Expression captures, ParameterInfo parameter)
        {
            if (parameter.IsOptional)
            {
                // If it's optional it can't be a body parameter, so assign it
                // directly:
                //
                // object localValue;
                // if (captures.TryGetValue("parameter", out localValue))
                //     parameter = (T)localValue
                // else
                //     parameter = DefaultValue
                this.body.Add(
                    Expression.Assign(
                        parameterValue,
                        Expression.Condition(
                            Expression.Call(
                                captures,
                                this.dictionaryTryGetValue,
                                Expression.Constant(parameter.Name),
                                this.localValue),
                            Expression.Convert(this.localValue, parameter.ParameterType),
                            GetDefaultValue(parameter))));
            }
            else
            {
                // localValue = captures["parameter"]
                Expression capturedValue = Expression.Call(
                    captures,
                    this.dictionaryGetValue,
                    Expression.Constant(parameter.Name));
                this.ConvertAndAssignValue(parameterValue, capturedValue, parameter.ParameterType);
            }
        }

        private void ConvertAndAssignValue(ParameterExpression parameter, Expression getValue, Type type)
        {
            // object value = ??
            // IValueProvider provider = value as IValueProvider
            this.body.Add(Expression.Assign(this.localValue, getValue));
            this.body.Add(Expression.Assign(
                this.localValueProvider,
                Expression.TypeAs(this.localValue, typeof(IValueProvider))));

            // if (provider == null)
            //     parameter = (T)value
            // else
            //     parameter = (T)provider.Value
            this.body.Add(
                Expression.Assign(
                    parameter,
                    Expression.Condition(
                        Expression.Equal(this.localValueProvider, Expression.Constant(null)),
                        Expression.Convert(this.localValue, type),
                        Expression.Convert(
                            Expression.Property(this.localValueProvider, this.valueProviderGetValue),
                            type))));
        }

        private Expression CreateInstance(Expression captures, Expression instance, Func<object> factory)
        {
            Expression createInstance;
            if (factory != null)
            {
                // (T)factory()
                createInstance = Expression.Convert(
                    Expression.Invoke(Expression.Constant(factory)),
                    instance.Type);
            }
            else
            {
                // (IServiceProvider)captures["ServiceProviderKey"]
                Expression getServiceProvider =
                    Expression.Convert(
                        Expression.Call(
                            captures,
                            this.dictionaryGetValue,
                            Expression.Constant(ServiceProviderPlaceholder.Key)),
                        typeof(IServiceProvider));

                // (T)serviceProvider.GetService(typeof(T))
                createInstance = Expression.Convert(
                    Expression.Call(
                        getServiceProvider,
                        this.serviceProviderGetservice,
                        Expression.Constant(instance.Type)),
                    instance.Type);
            }

            // T instance = (T)factory()
            return Expression.Assign(instance, createInstance);
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
            var parameterExpressions = new Expression[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterExpressions[i] = locals[i];
            }

            Expression callMethod = Expression.Call(instance, method, parameterExpressions);
            MethodInfo convertMethod = this.GetConvertReturnTypeMethod(method.ReturnType);
            return Expression.Call(convertMethod, callMethod);
        }
    }
}
