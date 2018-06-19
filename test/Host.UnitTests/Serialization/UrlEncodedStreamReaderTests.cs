namespace Host.UnitTests.Serialization
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class UrlEncodedStreamReaderTests
    {
        private static T ReadValue<T>(string text, Func<UrlEncodedStreamReader, T> readMethod)
        {
            T result = default;
            WithReader(text, r =>
            {
                result = readMethod(r);
            });

            return result;
        }

        private static void WithReader(string text, Action<UrlEncodedStreamReader> action)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            using (var stream = new MemoryStream(bytes, writable: false))
            {
                var reader = new UrlEncodedStreamReader(stream);
                action(reader);
            }
        }

        public sealed class Constructor : UrlEncodedStreamReaderTests
        {
            [Theory]
            [InlineData("%")]
            [InlineData("%1")]
            [InlineData("%1G")]
            [InlineData("%g1")]
            public void ShouldThrowForInvalidEscapeSequences(string sequence)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sequence);
                using (var stream = new MemoryStream(bytes, writable: false))
                {
                    Action action = () => new UrlEncodedStreamReader(stream);

                    action.Should().Throw<FormatException>();
                }
            }
        }

        public sealed class GetCurrentPosition : UrlEncodedStreamReaderTests
        {
            [Fact]
            public void ShouldReturnTheKeyInformation()
            {
                WithReader("keyName=value", r =>
                {
                    r.ReadString();

                    string position = r.GetCurrentPosition();

                    position.Should().Contain("keyName");
                });
            }
        }

        public sealed class MoveToNextSibling : UrlEncodedStreamReaderTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheNextPartDoesNotHaveTheSamePrefix()
            {
                WithReader("one.two=x&three.four=x", r =>
                {
                    r.MoveToChildren();

                    bool result = r.MoveToNextSibling();

                    result.Should().BeFalse();
                });
            }

            [Fact]
            public void ShouldReturnFalseWhenAtTheEnd()
            {
                WithReader("one=value", r =>
                {
                    bool result = r.MoveToNextSibling();

                    result.Should().BeFalse();
                });
            }
        }

        public sealed class ReadBoolean : UrlEncodedStreamReaderTests
        {
            [Fact]
            public void ShouldReturnFalseForTheFalseToken()
            {
                bool result = ReadValue("key=false", r => r.ReadBoolean());

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForTheTrueToken()
            {
                bool result = ReadValue("key=true", r => r.ReadBoolean());

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldThrowForInvalidTokens()
            {
                Action action = () => ReadValue("key=invalid", r => r.ReadBoolean());

                // Make sure the exception includes where it was
                action.Should().Throw<FormatException>().WithMessage("*key*");
            }
        }

        public sealed class ReadByte : UrlEncodedStreamReaderTests
        {
            [Fact]
            public void ShouldIgnoreWhiteSpace()
            {
                byte result = ReadValue("key= 123 ", r => r.ReadByte());

                result.Should().Be(123);
            }
        }

        public sealed class ReadChar : UrlEncodedStreamReaderTests
        {
            [Theory]
            [InlineData("X", 'X')]
            [InlineData("%3d", '=')]
            [InlineData("%3D", '=')]
            public void ShouldReturnTheCharacter(string value, char expected)
            {
                char result = ReadValue("key=" + value, r => r.ReadChar());

                result.Should().Be(expected);
            }

            [Fact]
            public void ShouldThrowForMissingCharacters()
            {
                Action action = () => ReadValue("key=", r => r.ReadChar());

                action.Should().Throw<FormatException>();
            }

            [Fact]
            public void ShouldThrowForTooManyCharacters()
            {
                Action action = () => ReadValue("key=invalid", r => r.ReadChar());

                action.Should().Throw<FormatException>();
            }
        }

        public sealed class ReadNull : UrlEncodedStreamReaderTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheTokenIsNotNull()
            {
                bool result = ReadValue("key=value", r => r.ReadNull());

                result.Should().Be(false);
            }

            [Fact]
            public void ShouldReturnTrueIfTheTokenIsNull()
            {
                bool result = ReadValue("key=null", r => r.ReadNull());

                result.Should().Be(true);
            }
        }

        public sealed class ReadString : XmlStreamReaderTests
        {
            [Fact]
            public void ShouldReadEmptyValues()
            {
                string result = ReadValue("key=", r => r.ReadString());

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReadTheValue()
            {
                string result = ReadValue("key=string+value", r => r.ReadString());

                result.Should().Be("string value");
            }
        }
    }
}
