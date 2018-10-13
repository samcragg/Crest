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
    using System.Runtime.CompilerServices;
    using Microsoft.CSharp.RuntimeBinder;

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

        private readonly object source;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSource"/> class.
        /// </summary>
        /// <param name="source">The source of the data.</param>
        public DataSource(object source)
        {
            this.source = source;
        }

        /// <summary>
        /// Gets the member names of the data source.
        /// </summary>
        /// <returns>A sequence of member names from the data source.</returns>
        public virtual IEnumerable<string> GetMembers()
        {
            if (this.source is IReadOnlyDictionary<string, string[]> dictionary)
            {
                return dictionary.Keys;
            }
            else if (this.source is IDynamicMetaObjectProvider provider)
            {
                return provider.GetMetaObject(Expression.Constant(this.source))
                    .GetDynamicMemberNames();
            }
            else
            {
                return this.source.GetType().GetProperties().Select(p => p.Name);
            }
        }

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
    }
}
