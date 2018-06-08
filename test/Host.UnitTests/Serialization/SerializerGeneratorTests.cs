namespace Host.UnitTests.Serialization
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
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

        public enum TestEnum
        {
            Value = 123
        }

        private SerializerGenerator<_SerializerBase> Generator => this.generator.Value;

        public class _SerializerBase : FakeSerializerBase
        {
            internal static Type LastGeneratedType;

            protected _SerializerBase(Stream stream, SerializationMode mode)
                : base(stream, mode)
            {
                StreamWriter = this.Writer;
                LastGeneratedType = this.GetType();
                SetupSerializer?.Invoke(this);
                SetupSerializer = null;
            }

            protected _SerializerBase(_SerializerBase parent)
                : base(parent)
            {
                LastGeneratedType = this.GetType();
            }

            // Make this specific to this class to allow other tests to run
            // in parallel
            public static new bool OutputEnumNames { get; set; }

            internal static Action<FakeSerializerBase> SetupSerializer { get; set; }

            internal static ValueWriter StreamWriter { get; private set; }
        }

        public sealed class Deserialize : SerializerGeneratorTests
        {
            [Fact]
            public void ShouldDeserializeArrays()
            {
                _SerializerBase.SetupSerializer =
                    s => s.SetArray(r => r.ReadInt32(), 1, 2, 3);

                object result = this.Generator.Deserialize(Stream.Null, typeof(int[]));

                result.Should().BeOfType<int[]>()
                      .Which.Should().Equal(1, 2, 3);
            }

            [Fact]
            public void ShouldDeserializeEnumsAsStrings()
            {
                _SerializerBase.OutputEnumNames = true;
                _SerializerBase.SetupSerializer =
                    s => s.Reader.ReadString().Returns(nameof(TestEnum.Value));

                object result = this.Generator.Deserialize(Stream.Null, typeof(TestEnum));

                result.Should().BeOfType<TestEnum>()
                      .Which.Should().Be(TestEnum.Value);
            }

            [Fact]
            public void ShouldDeserializeEnumsAsValues()
            {
                _SerializerBase.OutputEnumNames = false;
                _SerializerBase.SetupSerializer =
                    s => s.Reader.ReadInt32().Returns((int)TestEnum.Value);

                object result = this.Generator.Deserialize(Stream.Null, typeof(TestEnum));

                result.Should().BeOfType<TestEnum>()
                      .Which.Should().Be(TestEnum.Value);
            }

            [Fact]
            public void ShouldDeserializePrimitives()
            {
                _SerializerBase.SetupSerializer =
                    s => s.Reader.ReadInt32().Returns(123);

                object result = this.Generator.Deserialize(Stream.Null, typeof(int));

                result.Should().BeOfType<int>()
                      .Which.Should().Be(123);
            }
        }

        public sealed class GetSerializerFor : SerializerGeneratorTests
        {
            [Fact]
            public void ShouldEnsureThereAreNoCyclicDependencies()
            {
                this.generator.Value.Invoking(x => x.GetSerializerFor(typeof(CyclicReference)))
                    .Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldEnsureTheTypeIsAReferenceType()
            {
                this.generator.Value.Invoking(x => x.GetSerializerFor(typeof(InvalidStruct)))
                    .Should().Throw<InvalidOperationException>();
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

                action.Should().Throw<InvalidOperationException>()
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
            [Fact]
            public void ShouldCacheTheGeneratedTypes()
            {
                // We wont run in parallel with any other test that will touch
                // the static variable
                this.Generator.Serialize(Stream.Null, new SimpleType());
                Type first = _SerializerBase.LastGeneratedType;

                this.Generator.Serialize(Stream.Null, new SimpleType());
                Type second = _SerializerBase.LastGeneratedType;

                second.Should().Be(first);
            }

            [Fact]
            public void ShouldEnsureThereAreNoCyclicDependencies()
            {
                this.Generator.Invoking(x => x.Serialize(Stream.Null, new CyclicReference()))
                    .Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldSerializeArrays()
            {
                this.Generator.Serialize(Stream.Null, new[] { 1, 2, 3 });

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

                this.Generator.Serialize(Stream.Null, TestEnum.Value);

                _SerializerBase.StreamWriter.Received().WriteString(nameof(TestEnum.Value));
            }

            [Fact]
            public void ShouldSerializeEnumsAsValues()
            {
                _SerializerBase.OutputEnumNames = false;

                this.Generator.Serialize(Stream.Null, TestEnum.Value);

                _SerializerBase.StreamWriter.Received().WriteInt32((int)TestEnum.Value);
            }

            [Fact]
            public void ShouldSerializePrimitives()
            {
                this.Generator.Serialize(Stream.Null, 123);

                _SerializerBase.StreamWriter.Received().WriteInt32(123);
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
