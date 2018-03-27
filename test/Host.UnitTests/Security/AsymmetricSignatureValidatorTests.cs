namespace Host.UnitTests.Security
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Crest.Host.Security;
    using FluentAssertions;
    using Xunit;

    public class AsymmetricSignatureValidatorTests
    {
        private readonly FakeAsynnetricSignature validator = new FakeAsynnetricSignature();

        public sealed class IsValid : AsymmetricSignatureValidatorTests
        {
            [Fact]
            public void ShouldHashTheData()
            {
                byte[] data = Encoding.UTF8.GetBytes("Test Data");
                byte[] sha512Hash = Convert.FromBase64String("Q55M7tkxL+8uVUBCw9J9asMdqc9yuoZrqbDgAyjQYoB5dIK/LNAH4ICCltsLmHtz/h+VPpfiWIMmO5eDUTwpSQ==");

                this.validator.IsValid(data, new byte[0], HashAlgorithmName.SHA512);

                this.validator.Hash.Should().Equal(sha512Hash);
            }

            [Fact]
            public void ShouldReturnFalseForUnknownHashSchemes()
            {
                bool result = this.validator.IsValid(new byte[0], new byte[0], HashAlgorithmName.MD5);

                this.validator.Hash.Should().BeNull();
                result.Should().BeFalse();
            }
        }

        private class FakeAsynnetricSignature : AsymmetricSignatureValidator
        {
            public override string AlgorithmPrefix => "TEST";

            internal byte[] Hash { get; private set; }

            protected override bool ValidateHash(byte[] hash, byte[] signature, HashAlgorithmName algorithm)
            {
                this.Hash = hash;
                return true;
            }
        }
    }
}
