// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Security.Cryptography;

    /// <content>
    /// Contains the nested <see cref="JwtInformation"/> class.
    /// </content>
    internal partial class JwtSignatureVerifier
    {
        private sealed class JwtInformation
        {
            public string Alg { get; set; }

            public string Typ { get; set; }

            internal HashAlgorithmName AlgorithmName { get; set; }

            internal int PayloadStart { get; set; }

            internal int SignatureStart { get; set; }

            internal ISignatureValidator Validator { get; set; }
        }
    }
}
