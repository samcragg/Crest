// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    /// <summary>
    /// Generates code to execute code based on a condition being true.
    /// </summary>
    internal sealed partial class JumpTableGenerator
    {
        private readonly Func<string, int> getHashCode;
        private readonly List<Mapping> mappings = new List<Mapping>();

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
            21023, 25229, 30293, 36353
        };

        private ILGenerator generator;

        /// <summary>
        /// Initializes a new instance of the <see cref="JumpTableGenerator"/> class.
        /// </summary>
        /// <param name="getHashCode">
        /// Used to generate the hash code when generating the jump table.
        /// </param>
        public JumpTableGenerator(Func<string, int> getHashCode)
        {
            this.getHashCode = getHashCode;
        }

        /// <summary>
        /// Gets or sets the action to perform that emits the condition check.
        /// </summary>
        /// <remarks>
        /// The action will be passed the generator that should be used to emit
        /// the instructions to and the key that is being compared. It should
        /// leave a value on the evaluation stack that can be used to branch if
        /// false (e.g. 0/null for false)
        /// </remarks>
        public Action<ILGenerator, string> EmitCondition { get; set; }

        /// <summary>
        /// Gets or sets the action to perform to emit code that places the hash
        /// code of the item being compared.
        /// </summary>
        /// <remarks>
        /// The function should place an integer representing the hash code of
        /// the item being compared onto the evaluation stack.
        /// </remarks>
        public Action<ILGenerator> EmitGetHashCode { get; set; }

        /// <summary>
        /// Gets or sets the label to jump to after comparison is completed.
        /// </summary>
        public Label EndOfTable { get; set; }

        /// <summary>
        /// Gets or sets the action to perform to emit the code that is executed
        /// if non of the conditions match.
        /// </summary>
        public Action<ILGenerator> NoMatch { get; set; }

        /// <summary>
        /// Emits a jump table for all the key/code mappings.
        /// </summary>
        /// <param name="generator">Used to generate the code at runtime.</param>
        public void EmitJumpTable(ILGenerator generator)
        {
            this.generator = generator;

            // Roslyn uses 7, but benchmarking using our hashing function/
            // comparer shows gains at 4
            Label noMatchLabel = this.generator.DefineLabel();
            if (this.mappings.Count < 4)
            {
                this.EmitComparisonBranching(this.mappings, noMatchLabel);
            }
            else
            {
                this.EmitSwitchTable(noMatchLabel);
            }

            // Note we jump to the end as we could be nested inside a switch
            // table so end isn't guaranteed (or expected) to be after the
            // final else clause
            //
            // else
            //     EmitDiagnostic
            //     goto end
            this.generator.MarkLabel(noMatchLabel);
            this.NoMatch(this.generator);
            this.generator.Emit(OpCodes.Br, this.EndOfTable);
        }

        /// <summary>
        /// Adds a mapping between a key and a block of code.
        /// </summary>
        /// <param name="key">The key to match.</param>
        /// <param name="body">
        /// Generates the code to execute if a match is successful.
        /// </param>
        public void Map(string key, Action<ILGenerator> body)
        {
            this.mappings.Add(new Mapping
            {
                Body = body,
                HashCode = (uint)this.getHashCode(key),
                Key = key
            });
        }

        private Label[] CreateSwitchLabels(IReadOnlyList<Mapping>[] buckets, Label noMatchLabel)
        {
            var labels = new Label[buckets.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                if (buckets[i].Count == 0)
                {
                    labels[i] = noMatchLabel;
                }
                else
                {
                    labels[i] = this.generator.DefineLabel();
                }
            }

            return labels;
        }

        private Label EmitBranch(Mapping mapping, Label previousTarget, Label nextJump)
        {
            // If the previous condition is false it will jump here
            this.generator.MarkLabel(previousTarget);

            // if not condition then goto next
            this.EmitCondition(this.generator, mapping.Key);
            this.generator.Emit(OpCodes.Brfalse, nextJump);

            // body
            // goto end
            mapping.Body(this.generator);
            this.generator.Emit(OpCodes.Br, this.EndOfTable);

            return nextJump;
        }

        private void EmitComparisonBranching(IReadOnlyList<Mapping> entries, Label noMatchLabel)
        {
            Label previousJumpTarget = this.generator.DefineLabel();
            for (int i = 0; i < entries.Count - 1; i++)
            {
                previousJumpTarget = this.EmitBranch(
                    entries[i],
                    previousJumpTarget,
                    this.generator.DefineLabel());
            }

            // If the last comparison is false we jump to the no match label
            this.EmitBranch(entries[entries.Count - 1], previousJumpTarget, noMatchLabel);
        }

        private void EmitSwitchTable(Label noMatchLabel)
        {
            int bucketCount = this.FindPrime(this.mappings.Count);
            IReadOnlyList<Mapping>[] buckets = this.GetBuckets(bucketCount);
            Label[] labels = this.CreateSwitchLabels(buckets, noMatchLabel);

            // Since we have a case for each bucket, no need to worry about
            // the default case
            //
            // switch GetHashCode % buckets
            this.EmitGetHashCode(this.generator);
            this.generator.EmitLoadConstant(bucketCount);
            this.generator.Emit(OpCodes.Rem_Un);
            this.generator.Emit(OpCodes.Switch, labels);

            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i].Count > 0)
                {
                    this.generator.MarkLabel(labels[i]);
                    this.EmitComparisonBranching(buckets[i], noMatchLabel);
                }
            }
        }

        private int FindPrime(int count)
        {
            int prime = 3;
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

        private IReadOnlyList<Mapping>[] GetBuckets(int count)
        {
            var buckets = new List<Mapping>[count];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new List<Mapping>();
            }

            foreach (Mapping mapping in this.mappings)
            {
                buckets[mapping.HashCode % count].Add(mapping);
            }

            return buckets;
        }
    }
}
