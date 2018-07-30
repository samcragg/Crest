// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System;

    /// <summary>
    /// Allows the recording of a value at specific intervals.
    /// </summary>
    internal sealed class Gauge
    {
        private const double MicrosecondsPerMinute = 1000 * 1000 * 60; // μs/ms => sec => min
        private readonly ITimeProvider time;
        private long count;
        private double fifteenMinuteAverage;
        private double fiveMinuteAverage;
        private long lastTimestamp;
        private long maximum;
        private double mean;
        private long minimum;
        private double oneMinuteAverage;
        private double varianceSum;

        /// <summary>
        /// Initializes a new instance of the <see cref="Gauge"/> class.
        /// </summary>
        /// <param name="time">Used to provide the current system time.</param>
        public Gauge(ITimeProvider time)
        {
            this.time = time;
        }

        /// <summary>
        /// Gets the exponential moving average of the values over a fifteen
        /// minute window.
        /// </summary>
        public double FifteenMinuteAverage => this.fifteenMinuteAverage;

        /// <summary>
        /// Gets the exponential moving average of the values over a five minute
        /// window.
        /// </summary>
        public double FiveMinuteAverage => this.fiveMinuteAverage;

        /// <summary>
        /// Gets the largest value that has been added to this instance.
        /// </summary>
        public long Maximum => this.maximum;

        /// <summary>
        /// Gets the average of all the values added to this instance.
        /// </summary>
        public double Mean => this.mean;

        /// <summary>
        /// Gets the smallest value that has been added to this instance.
        /// </summary>
        public long Minimum => this.minimum;

        /// <summary>
        /// Gets the exponential moving average of the values over a one minute
        /// window.
        /// </summary>
        public double OneMinuteAverage => this.oneMinuteAverage;

        /// <summary>
        /// Gets the number of values that have been added to this instance.
        /// </summary>
        public long SampleSize => this.count;

        /// <summary>
        /// Gets the standard deviation from the mean of the values.
        /// </summary>
        public double StandardDeviation => Math.Sqrt(this.Variance);

        /// <summary>
        /// Gets the variance from the mean of the values.
        /// </summary>
        public double Variance
        {
            get
            {
                if (this.count < 2)
                {
                    return 0;
                }
                else
                {
                    return this.varianceSum / (this.count - 1);
                }
            }
        }

        /// <summary>
        /// Adds the specified value to the series of values.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(long value)
        {
            this.count++;
            long elapsed = this.GetElapsedMicroseconds();
            this.UpdateMovingWindow(value, elapsed, 1 * MicrosecondsPerMinute, ref this.oneMinuteAverage);
            this.UpdateMovingWindow(value, elapsed, 5 * MicrosecondsPerMinute, ref this.fiveMinuteAverage);
            this.UpdateMovingWindow(value, elapsed, 15 * MicrosecondsPerMinute, ref this.fifteenMinuteAverage);

            this.UpdateMinimumMaximum(value);
            this.UpdateAverages(value);
        }

        private long GetElapsedMicroseconds()
        {
            long previous = this.lastTimestamp;
            long current = this.time.GetCurrentMicroseconds();
            this.lastTimestamp = current;

            long delta = current - previous;
            return delta < 0 ? 0 : delta;
        }

        private void UpdateAverages(long value)
        {
            // https://en.wikipedia.org/wiki/Standard_deviation#Rapid_calculation_methods
            //
            //             Xk - Ak-1
            // Ak = Ak-1 + ---------
            //                 k
            double previousMean = this.mean;
            this.mean += (value - this.mean) / this.count;

            // Qk = Qk-1 + (Xk - Ak-1)(Xk - Ak)
            this.varianceSum += (value - previousMean) * (value - this.mean);
        }

        private void UpdateMinimumMaximum(long value)
        {
            if (this.count == 1)
            {
                this.maximum = value;
                this.minimum = value;
            }
            else
            {
                if (value < this.minimum)
                {
                    this.minimum = value;
                }

                if (value > this.maximum)
                {
                    this.maximum = value;
                }
            }
        }

        private void UpdateMovingWindow(long value, long elapsedMicroseconds, double windowMicroseconds, ref double current)
        {
            // Special case for the first time, which should be seeded with the
            // actual value
            if (this.count == 1)
            {
                current = value;
            }
            else
            {
                // https://en.wikipedia.org/wiki/Moving_average#Application_to_measuring_computer_performance
                //       /       / Tn - Tn-1\\             / Tn - Tn-1\
                // Sn = |1 - exp|- --------- || * Yn + exp|- --------- | * Sn-1
                //       \       \   W * 60 //             \   W * 60 /
                //
                // W is the windows in minutes and t is in seconds, hence the
                // W * 60 in the above but we don't need that
                double exp = Math.Exp(-(elapsedMicroseconds / windowMicroseconds));
                current = ((1 - exp) * value) + (exp * current);
            }
        }
    }
}
