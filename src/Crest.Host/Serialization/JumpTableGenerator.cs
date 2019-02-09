// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Generates code to execute code based on a condition being true.
    /// </summary>
    internal sealed partial class JumpTableGenerator
    {
        private readonly List<Mapping> mappings = new List<Mapping>();
        private readonly Methods methods;

        // Values stolen from https://referencesource.microsoft.com/#mscorlib/system/collections/hashtable.cs
        //
        // Note we don't need smaller than 7, as up to 4 properties will not
        // be using a hash lookup. Also note that we stop after 2^15, as
        // https://stackoverflow.com/a/12015108/312325 states there can be a
        // maximum of 2^16 methods, however, there will be one method for the
        // getter and one for the setter so it will be half the maximum
        private readonly int[] primes =
        {
            7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293,
            353, 431, 521, 631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801,
            3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519,
            21023, 25229, 30293, 36353,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="JumpTableGenerator"/> class.
        /// </summary>
        /// <param name="methods">Contains the methods information.</param>
        public JumpTableGenerator(Methods methods)
        {
            this.methods = methods;
        }

        /// <summary>
        /// Adds a mapping between a key and a block of code.
        /// </summary>
        /// <param name="key">The key to match.</param>
        /// <param name="expression">
        /// The expression to execute if a match is successful.
        /// </param>
        public void Add(string key, Expression expression)
        {
            this.mappings.Add(new Mapping(key, expression));
        }

        /// <summary>
        /// Creates an expression maps keys to expressions.
        /// </summary>
        /// <param name="variable">The variable to test the value of.</param>
        /// <returns>A new expression.</returns>
        public Expression Build(ParameterExpression variable)
        {
            // Roslyn uses 7, but benchmarking using our hashing function/
            // comparer shows gains at 4
            if (this.mappings.Count < 4)
            {
                // Straight forward if cases
                return this.ComparisonTesting(variable, this.mappings);
            }
            else
            {
                return this.GetSwitchExpression(variable);
            }
        }

        private Expression ComparisonTesting(ParameterExpression variable, IEnumerable<Mapping> entries)
        {
            Expression expression = Expression.Empty();
            foreach (Mapping entry in entries)
            {
                expression = Expression.IfThenElse(
                    Expression.Call(
                        this.methods.CaseInsensitiveStringHelper.Equals,
                        Expression.Constant(entry.Key),
                        variable),
                    entry.Body,
                    expression);
            }

            return expression;
        }

        private int FindPrime(int count)
        {
            int prime = 0;
            for (int i = 0; i < this.primes.Length; i++)
            {
                prime = this.primes[i];
                if (prime > count)
                {
                    break;
                }
            }

            return prime;
        }

        private Expression GetSwitchExpression(ParameterExpression variable)
        {
            var switchCases = new List<SwitchCase>();
            int bucketCount = this.FindPrime(this.mappings.Count);
            ILookup<int, Mapping> buckets = this.mappings.ToLookup(m => m.HashCode % bucketCount);
            foreach (IGrouping<int, Mapping> bucket in buckets)
            {
                switchCases.Add(Expression.SwitchCase(
                    this.ComparisonTesting(variable, bucket),
                    Expression.Constant(bucket.Key)));
            }

            Expression hashCode = Expression.Call(
                this.methods.CaseInsensitiveStringHelper.GetHashCode,
                variable);
            return Expression.Switch(
                Expression.Modulo(hashCode, Expression.Constant(bucketCount)),
                switchCases.ToArray());
        }
    }
}
