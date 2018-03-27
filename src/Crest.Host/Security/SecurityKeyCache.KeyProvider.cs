// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    /// <content>
    /// Contains the nested <see cref="KeyProvider"/> class.
    /// </content>
    internal partial class SecurityKeyCache
    {
        private class KeyProvider
        {
            public KeyProvider(ISecurityKeyProvider provider)
            {
                this.Provider = provider;
                this.Version = int.MaxValue;
            }

            public ECParameters[] EC { get; set; }

            public ISecurityKeyProvider Provider { get; }

            public RSAParameters[] Rsa { get; set; }

            public byte[][] SecretKeys { get; set; }

            public Task UpdateTask { get; set; }

            public int Version { get; set; }
        }
    }
}
