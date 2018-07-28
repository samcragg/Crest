namespace Host.UnitTests.TestHelpers
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using Xunit.Sdk;

    /// <summary>
    /// Replace the <see cref="CultureInfo.CurrentCulture" /> with another
    /// culture for a test.
    /// </summary>
    // https://github.com/xunit/samples.xunit/blob/master/UseCulture/UseCultureAttribute.cs
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class UseCultureAttribute : BeforeAfterTestAttribute
    {
        private readonly CultureInfo culture;
        private CultureInfo original;

        /// <summary>
        /// Replaces the culture of the current thread with the specified
        /// culture.
        /// </summary>
        /// <param name="culture">The name of the culture.</param>
        public UseCultureAttribute(string culture)
        {
            this.culture = new CultureInfo(culture, useUserOverride: false);
        }

        /// <inheritdoc />
        public override void After(MethodInfo methodUnderTest)
        {
            Thread.CurrentThread.CurrentCulture = this.original;
            CultureInfo.CurrentCulture.ClearCachedData();
        }

        /// <inheritdoc />
        public override void Before(MethodInfo methodUnderTest)
        {
            this.original = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = this.culture;
            CultureInfo.CurrentCulture.ClearCachedData();
        }
    }
}
