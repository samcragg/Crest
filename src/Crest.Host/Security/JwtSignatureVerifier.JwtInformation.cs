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
            public string Alg { get; private set; }

            public string Typ { get; private set; }

            public HashAlgorithmName AlgorithmName { get; set; }

            public int PayloadStart { get; set; }

            public int SignatureStart { get; set; }

            public ISignatureValidator Validator { get; set; }

            public void SetProperty(string key, string value)
            {
                switch (key)
                {
                    case "alg":
                        this.Alg = value;
                        break;

                    case "typ":
                        this.Typ = value;
                        break;
                }
            }
        }
    }
}
