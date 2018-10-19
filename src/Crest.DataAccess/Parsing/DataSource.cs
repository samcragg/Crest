// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Microsoft.CSharp.RuntimeBinder;
    using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

    /// <summary>
    /// Extracts the properties and values from an object.
    /// </summary>
    internal class DataSource
    {
        private static readonly CSharpArgumentInfo[] ArgumentInfo =
        {
            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
        };

        private static readonly Dictionary<string, Func<object, object>> Getters =
            new Dictionary<string, Func<object, object>>(StringComparer.OrdinalIgnoreCase);

        private readonly IReadOnlyCollection<string> members;
        private readonly object source;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSource"/> class.
        /// </summary>
        /// <param name="source">The source of the data.</param>
        public DataSource(object source)
        {
            this.members = GetMembers(source);
            this.source = source;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSource"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is only used to allow the type to be mocked in unit tests.
        /// </remarks>
        protected DataSource()
        {
        }

        /// <summary>
        /// Gets the member names of the data source.
        /// </summary>
        public virtual IReadOnlyCollection<string> Members => this.members;

        /// <summary>
        /// Gets the value of the member from the data source.
        /// </summary>
        /// <param name="member">The name of the member.</param>
        /// <returns>The value represented by the member.</returns>
        public virtual object GetValue(string member)
        {
            if (this.source is IReadOnlyDictionary<string, string[]> dictionary)
            {
                return dictionary[member];
            }
            else
            {
                Func<object, object> getter = GetAccessor(this.source.GetType(), member);
                return getter(this.source);
            }
        }

        private static Func<object, object> GetAccessor(Type type, string member)
        {
            string fullName = type.FullName + "." + member;
            if (!Getters.TryGetValue(fullName, out Func<object, object> getter))
            {
                var callSite = CallSite<Func<CallSite, object, object>>.Create(
                     Binder.GetMember(
                         CSharpBinderFlags.None,
                         member,
                         type,
                         ArgumentInfo));

                getter = o => callSite.Target(callSite, o);
                lock (Getters)
                {
                    Getters[fullName] = getter;
                }
            }

            return getter;
        }

        private static IReadOnlyList<string> GetMembers(object value)
        {
            if (value is IReadOnlyDictionary<string, string[]> dictionary)
            {
                return MakeList(dictionary.Keys);
            }
            else if (value is IDynamicMetaObjectProvider provider)
            {
                return MakeList(
                    provider.GetMetaObject(Expression.Constant(value))
                            .GetDynamicMemberNames());
            }
            else
            {
                PropertyInfo[] properties = value.GetType().GetProperties();
                return Array.ConvertAll(properties, p => p.Name);
            }
        }

        private static IReadOnlyList<string> MakeList(IEnumerable<string> values)
        {
            // The majority of Dictionary implementations give us keys that are
            // actually collections, so try to use the original where possible
            return (values as IReadOnlyList<string>) ?? values.ToList();
        }
    }
}
