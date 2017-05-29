namespace Host.UnitTests.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Xunit;

    public class ExecutingAssemblyTests
    {
        private readonly ExecutingAssembly executingAssembly = new ExecutingAssembly(typeof(DiscoveryServiceTests).GetTypeInfo().Assembly);

        public sealed class GetCompileLibraries : ExecutingAssemblyTests
        {
            [Fact]
            public void ShouldReturnTheNameAndVersion()
            {
                IEnumerable<ExecutingAssembly.AssemblyInfo> result = this.executingAssembly.GetCompileLibraries();

                // Since we're using Fluent Assertions in our tests we know it
                // must be a library the test assembly is compiled agains...
                result.Should().Contain(ai => string.Equals(nameof(FluentAssertions), ai.Name, StringComparison.OrdinalIgnoreCase))
                      .Which.Version.Should().NotBeNullOrEmpty();
            }
        }

        public sealed class LoadCompileLibraries : ExecutingAssemblyTests
        {
            [Fact]
            public void ShouldReturnTheAssemblies()
            {
                // Again, we're using a known assembly to check it's worked
                Assembly fluentAssertions = typeof(AssertionExtensions).GetTypeInfo().Assembly;

                IEnumerable<Assembly> result = this.executingAssembly.LoadCompileLibraries();

                result.Should().Contain(fluentAssertions);
            }

            [Fact]
            public void ShouldHandleExceptionsWhenLoadingAssemblies()
            {
                this.executingAssembly.AssemblyLoad = _ => { throw new BadImageFormatException(); };

                // Use ToList to force evaluation
                List<Assembly> result = this.executingAssembly.LoadCompileLibraries().ToList();

                result.Should().BeEmpty();
            }
        }
    }
}
