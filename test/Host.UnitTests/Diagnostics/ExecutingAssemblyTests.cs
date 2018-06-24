namespace Host.UnitTests.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyModel;
    using Xunit;

    [Collection("ExecutingAssembly.DependencyContext")]
    public class ExecutingAssemblyTests
    {
        private readonly ExecutingAssembly executingAssembly = new ExecutingAssembly();

        public ExecutingAssemblyTests()
        {
            ExecutingAssembly.DependencyContext = DependencyContext.Load(
                typeof(ExecutingAssemblyTests).GetTypeInfo().Assembly);
        }

        public sealed class DependencyContextProperty : ExecutingAssemblyTests
        {
            [Fact]
            public void ShouldBeEqualToTheDefaultContextIfSetToNull()
            {
                ExecutingAssembly.DependencyContext = null;

                DependencyContext result = ExecutingAssembly.DependencyContext;

                result.Should().BeSameAs(DependencyContext.Default);
            }
        }

        public sealed class GetCompileLibraries : ExecutingAssemblyTests
        {
            [Fact]
            public void ShouldReturnTheNameAndVersion()
            {
                IEnumerable<ExecutingAssembly.AssemblyInfo> result = this.executingAssembly.GetCompileLibraries();

                // Since we're using Fluent Assertions in our tests we know it
                // must be a library the test assembly is compiled against...
                result.Should().Contain(ai => string.Equals(nameof(FluentAssertions), ai.Name, StringComparison.OrdinalIgnoreCase))
                      .Which.Version.Should().NotBeNullOrEmpty();
            }
        }

        public sealed class LoadCompileLibraries : ExecutingAssemblyTests
        {
            [Fact]
            public void ShouldHandleExceptionsWhenLoadingAssemblies()
            {
                this.executingAssembly.AssemblyLoad = _ => { throw new BadImageFormatException(); };

                // Use ToList to force evaluation
                List<Assembly> result = this.executingAssembly.LoadCompileLibraries().ToList();

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnTheAssemblies()
            {
                // Again, we're using a known assembly to check it's worked
                Assembly fluentAssertions = typeof(AssertionExtensions).GetTypeInfo().Assembly;

                IEnumerable<Assembly> result = this.executingAssembly.LoadCompileLibraries();

                result.Should().Contain(fluentAssertions);
            }
        }
    }
}
