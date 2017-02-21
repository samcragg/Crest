// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using static System.Diagnostics.Debug;

    /// <content>
    /// Contains the nested helper <see cref="ObjectWalker"/> class.
    /// </content>
    internal partial class HtmlConverter
    {
        private class ObjectWalker
        {
            private readonly HashSet<object> visited = new HashSet<object>(new ObjectComparer());
            private readonly TextWriter writer;
            private string indent = string.Empty;

            internal ObjectWalker(TextWriter writer)
            {
                this.writer = writer;
            }

            public void WriteObject(object instance)
            {
                if (instance == null)
                {
                    this.Write("<var>null</var>");
                }
                else if (this.visited.Add(instance))
                {
                    this.WriteValue(instance);
                }
                else
                {
                    // We've already printed the value once, avoid an infinite loop
                    this.Write("...");
                }
            }

            private void Indent()
            {
                this.indent += "  ";
            }

            private void Unindent()
            {
                Assert(this.indent.Length >= 2, "Indent must be called before Unindent.");
                this.indent = this.indent.Substring(0, this.indent.Length - 2);
            }

            private void Write(string value)
            {
                this.writer.Write(this.indent);
                this.writer.WriteLine(value);
            }

            private void WriteList(IEnumerable enumerable)
            {
                int index = 0;
                foreach (object item in enumerable)
                {
                    this.Write("[" + index + "]");
                    index++;

                    this.Indent();
                    this.WriteObject(item);
                    this.Unindent();
                }
            }

            private void WriteProperties(object instance)
            {
                IEnumerable<PropertyInfo> properties =
                    instance.GetType()
                            .GetProperties()
                            .Where(pi => pi.CanRead)
                            .OrderBy(pi => pi.Name);

                foreach (PropertyInfo property in properties)
                {
                    this.Write(property.Name + ":");
                    this.Indent();
                    this.WriteObject(property.GetValue(instance));
                    this.Unindent();
                }
            }

            private void WriteValue(object instance)
            {
                var convertible = instance as IConvertible;
                if (convertible != null)
                {
                    this.Write(convertible.ToString(CultureInfo.CurrentCulture));
                }
                else
                {
                    var enumerable = instance as IEnumerable;
                    if (enumerable != null)
                    {
                        this.WriteList(enumerable);
                    }
                    else
                    {
                        this.WriteProperties(instance);
                    }
                }
            }

            // We're interested in preventing infinite loops by iterating over
            // the same reference of not, not whether they're semantically equal
            private class ObjectComparer : IEqualityComparer<object>
            {
                // Has to be explicit, as we inherit object.Equals(object, object)
                bool IEqualityComparer<object>.Equals(object x, object y)
                {
                    return object.ReferenceEquals(x, y);
                }

                int IEqualityComparer<object>.GetHashCode(object obj)
                {
                    return RuntimeHelpers.GetHashCode(obj);
                }
            }
        }
    }
}
