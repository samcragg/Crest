namespace Host.UnitTests.Serialization
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    // This attribute is inherited to prevent the tests running in parallel
    // due to the static ModuleBuilder property
    [Collection(nameof(SerializerGenerator.ModuleBuilder))]
    public class SerializerGeneratorTests
    {
        // We need to use lazy so that we can change the value of the
        // OutputEnumNames before the constructor is called
        private readonly Lazy<SerializerGenerator<_SerializerBase>> generator =
            new Lazy<SerializerGenerator<_SerializerBase>>();

        protected SerializerGeneratorTests()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("UnitTestDynamicAssembly"),
                AssemblyBuilderAccess.RunAndCollect);

            SerializerGenerator.ModuleBuilder = assemblyBuilder.DefineDynamicModule("Module");
        }

        public class _SerializerBase : FakeSerializerBase
        {
            internal static Type LastGeneratedType;

            protected _SerializerBase(Stream stream, SerializationMode mode)
                : base(stream, mode)
            {
                StreamWriter = this.Writer;
                LastGeneratedType = this.GetType();
            }

            protected _SerializerBase(_SerializerBase parent)
                : base(parent)
            {
                LastGeneratedType = this.GetType();
            }

            // Make this specific to this class to allow other tests to run
            // in parallel
            public static new bool OutputEnumNames { get; set; }

            internal static IStreamWriter StreamWriter { get; private set; }
        }

        public sealed class GetSerializerFor : SerializerGeneratorTests
        {
            [Fact]
            public void ShouldEnsureThereAreNoCyclicDependencies()
            {
                this.generator.Value.Invoking(x => x.GetSerializerFor(typeof(CyclicReference)))
                    .ShouldThrow<InvalidOperationException>();
            }

            [Fact]
            public void ShouldEnsureTheTypeIsAReferenceType()
            {
                this.generator.Value.Invoking(x => x.GetSerializerFor(typeof(InvalidStruct)))
                    .ShouldThrow<InvalidOperationException>();
            }

            [Fact]
            public void ShouldReturnTheSameSerializerForAnArrayOfAType()
            {
                Type serializer1 = this.generator.Value.GetSerializerFor(typeof(SimpleType));
                Type serializer2 = this.generator.Value.GetSerializerFor(typeof(SimpleType[]));

                serializer2.Should().BeSameAs(serializer1);
            }

            [Fact]
            public void ShouldReturnTheSameSerializerForTheSameType()
            {
                Type serializer1 = this.generator.Value.GetSerializerFor(typeof(SimpleType));
                Type serializer2 = this.generator.Value.GetSerializerFor(typeof(SimpleType));

                serializer2.Should().BeSameAs(serializer1);
            }

            public struct InvalidStruct
            {
            }

            public class CyclicReference
            {
                public CyclicReference Value { get; set; }
            }

            public class SimpleType
            {
                public int MyProperty { get; set; }
            }
        }

        public sealed class OutputEnumNames : SerializerGeneratorTests
        {
            [Fact]
            public void ShouldReturnTheValueFromTheBaseClass()
            {
                bool result = SerializerGenerator.OutputEnumNames(typeof(ReturnsTrue));

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldThrowIsNoPropertyExists()
            {
                Action action = () => SerializerGenerator.OutputEnumNames(typeof(MissingProperty));

                action.ShouldThrow<InvalidOperationException>()
                      .WithMessage("*MissingProperty*");
            }

            private class MissingProperty
            {
            }

            private class ReturnsTrue
            {
                public static bool OutputEnumNames => true;
            }
        }

        public sealed class Serialize : SerializerGeneratorTests
        {
            public enum TestEnum
            {
                Value = 123
            }

            [Fact]
            public void ShouldCacheTheGeneratedTypes()
            {
                // We wont run in parallel with any other test that will touch
                // the static variable
                this.generator.Value.Serialize(Stream.Null, new SimpleType());
                Type first = _SerializerBase.LastGeneratedType;

                this.generator.Value.Serialize(Stream.Null, new SimpleType());
                Type second = _SerializerBase.LastGeneratedType;

                second.Should().Be(first);
            }

            [Fact]
            public void ShouldEnsureThereAreNoCyclicDependencies()
            {
                this.generator.Value.Invoking(x => x.Serialize(Stream.Null, new CyclicReference()))
                    .ShouldThrow<InvalidOperationException>();
            }

            [Fact]
            public void ShouldSerializeArrays()
            {
                this.generator.Value.Serialize(Stream.Null, new[] { 1, 2, 3 });

                Received.InOrder(() =>
                {
                    _SerializerBase.StreamWriter.WriteInt32(1);
                    _SerializerBase.StreamWriter.WriteInt32(2);
                    _SerializerBase.StreamWriter.WriteInt32(3);
                });
            }

            [Fact]
            public void ShouldSerializeEnumsAsStrings()
            {
                _SerializerBase.OutputEnumNames = true;

                this.generator.Value.Serialize(Stream.Null, TestEnum.Value);

                _SerializerBase.StreamWriter.Received().WriteString(nameof(TestEnum.Value));
            }

            [Fact]
            public void ShouldSerializeEnumsAsValues()
            {
                _SerializerBase.OutputEnumNames = false;

                this.generator.Value.Serialize(Stream.Null, TestEnum.Value);

                _SerializerBase.StreamWriter.Received().WriteInt32((int)TestEnum.Value);
            }

            [Fact]
            public void ShouldSerializePrimitives()
            {
                this.generator.Value.Serialize(Stream.Null, 123);

                _SerializerBase.StreamWriter.Received().WriteInt32(123);
            }

            public struct InvalidStruct
            {
            }

            public class CyclicReference
            {
                public CyclicReference Value { get; set; }
            }

            public class SimpleType
            {
                public int MyProperty { get; set; }
            }
        }
    }
}
