// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;

    /// <summary>
    /// Provides the default value for the properties in a configuration class.
    /// </summary>
    internal sealed class DefaultConfigurationProvider : IConfigurationProvider
    {
        private readonly Dictionary<Type, Action<object>> initialize =
            new Dictionary<Type, Action<object>>();

        /// <inheritdoc />
        public int Order => 100;

        /// <inheritdoc />
        public Task InitializeAsync(IEnumerable<Type> knownTypes)
        {
            return Task.Run(() =>
            {
                foreach (Type type in knownTypes)
                {
                    Action<object> initialize = CreateInitializeDelegate(type);
                    if (initialize != null)
                    {
                        this.initialize.Add(type, initialize);
                    }
                }
            });
        }

        /// <inheritdoc />
        public void Inject(object instance)
        {
            if (this.initialize.TryGetValue(instance.GetType(), out Action<object> initialize))
            {
                initialize(instance);
            }
        }

        private static Action<object> CreateInitializeDelegate(Type type)
        {
            ParameterExpression instance = Expression.Parameter(type);
            Expression setProperties = SetDefaults(type, instance);
            if (setProperties == null)
            {
                return null;
            }

            ParameterExpression parameter = Expression.Parameter(typeof(object));
            Expression body = Expression.Block(
                new[] { instance },
                Expression.Assign(instance, Expression.Convert(parameter, type)),
                setProperties);

            return Expression.Lambda<Action<object>>(body, parameter).Compile();
        }

        private static Expression SetDefaults(Type type, Expression instance)
        {
            PropertyInfo[] properties = type.GetProperties();
            if (properties.Length > 0)
            {
                var body = new List<Expression>();
                var locals = new List<ParameterExpression>();
                for (int i = 0; i < properties.Length; i++)
                {
                    SetProperty(instance, body, locals, properties[i]);
                }

                // Only return an expression if there are any properties to set
                if (body.Count > 0)
                {
                    return Expression.Block(locals, body);
                }
            }

            return null;
        }

        private static void SetProperty(Expression instance, IList<Expression> body, IList<ParameterExpression> locals, PropertyInfo property)
        {
            DefaultValueAttribute defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultValue != null)
            {
                // Simple case - just set the property to the default value
                body.Add(Expression.Assign(
                    Expression.Property(instance, property),
                    Expression.Constant(defaultValue.Value)));
            }
            else if (!property.PropertyType.GetTypeInfo().IsValueType)
            {
                // We need to see if any of the nested properties need setting.
                // If they do we'll need to do a null check, so use a local
                // variable to store the value, however, since we don't know at
                // this stage whether any nested properties need setting don't
                // add the null check+local until we have something to set.
                ParameterExpression local = Expression.Parameter(property.PropertyType);
                Expression setNestedProperties = SetDefaults(property.PropertyType, local);
                if (setNestedProperties != null)
                {
                    // var local = instance.Property;
                    // if (local != null) { SetDefaults(local); }
                    locals.Add(local);
                    body.Add(Expression.Assign(local, Expression.Property(instance, property)));
                    body.Add(Expression.IfThen(
                        Expression.IsFalse(Expression.Equal(local, Expression.Constant(null))),
                        setNestedProperties));
                }
            }
        }
    }
}
