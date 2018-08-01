// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System;

    /// <summary>
    /// Allows the reporting of metric information.
    /// </summary>
    internal interface IReporter : IDisposable
    {
        /// <summary>
        /// Generates a string containing the report data.
        /// </summary>
        /// <returns>The generated report.</returns>
        string GenerateReport();

        /// <summary>
        /// Appends the specified counter information.
        /// </summary>
        /// <param name="label">The name of the counter.</param>
        /// <param name="counter">The counter information.</param>
        /// <param name="unit">The represented unit information.</param>
        void Write(string label, Counter counter, IUnit unit);

        /// <summary>
        /// Appends the specified gauge information.
        /// </summary>
        /// <param name="label">The name of the gauge.</param>
        /// <param name="gauge">The gauge information.</param>
        /// <param name="unit">The represented unit information.</param>
        void Write(string label, Gauge gauge, IUnit unit);
    }
}
