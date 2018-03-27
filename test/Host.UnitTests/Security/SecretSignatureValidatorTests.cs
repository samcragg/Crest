namespace Host.UnitTests.Security
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Crest.Host.Security;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class SecretSignatureValidatorTests
    {
        private readonly SecurityKeyCache cache;
        private readonly byte[] secretKey = new byte[] { 1, 2, 3 };
        private readonly SecretSignatureValidator validator;

        public SecretSignatureValidatorTests()
        {
            this.cache = Substitute.For<SecurityKeyCache>(new object[] { new ISecurityKeyProvider[0] });
            this.validator = new SecretSignatureValidator(this.cache);
            this.cache.GetSecretKeys().Returns(new[] { this.secretKey });
        }

        public sealed class AlgorithmPrefix : SecretSignatureValidatorTests
        {
            [Fact]
            public void ShouldReturnHS()
            {
                this.validator.AlgorithmPrefix
                    .Should().Be("HS");
            }
        }

        public sealed class IsValid : SecretSignatureValidatorTests
        {
            [Fact]
            public void ShouldHandleSHA384()
            {
                byte[] data = Encoding.UTF8.GetBytes("Test");
                byte[] signature = SignData(data, b => new HMACSHA384(b));

                bool result = this.validator.IsValid(data, signature, HashAlgorithmName.SHA384);

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldHandleSHA512()
            {
                byte[] data = Encoding.UTF8.GetBytes("Test");
                byte[] signature = SignData(data, b => new HMACSHA512(b));

                bool result = this.validator.IsValid(data, signature, HashAlgorithmName.SHA512);

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnFalseForIncorrectSizedSignatures()
            {
                bool result = this.validator.IsValid(new byte[0], new byte[1], HashAlgorithmName.SHA256);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForInvalidSignatures()
            {
                byte[] data = Encoding.UTF8.GetBytes("Test");
                byte[] signature = SignData(data);
                signature[0] = (byte)~signature[0];

                bool result = this.validator.IsValid(data, signature, HashAlgorithmName.SHA256);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForNullSignatures()
            {
                bool result = this.validator.IsValid(new byte[0], null, HashAlgorithmName.SHA256);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForUnknownAlgorithms()
            {
                bool result = this.validator.IsValid(new byte[0], new byte[128 / 8], HashAlgorithmName.MD5);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForValidSignatures()
            {
                byte[] data = Encoding.UTF8.GetBytes("Test");
                byte[] signature = SignData(data);

                bool result = this.validator.IsValid(data, signature, HashAlgorithmName.SHA256);

                result.Should().BeTrue();
            }

            private byte[] SignData(byte[] data, Func<byte[], HashAlgorithm> factory = null)
            {
                factory = factory ?? (b => new HMACSHA256(b));
                using (HashAlgorithm hmac = factory(this.secretKey))
                {
                    return hmac.ComputeHash(data);
                }
            }
        }
    }
}
