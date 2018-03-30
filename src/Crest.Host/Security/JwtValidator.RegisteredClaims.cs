// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Linq;

    /// <content>
    /// Contains the nested helper <see cref="RegisteredClaims"/> class.
    /// </content>
    internal partial class JwtValidator
    {
        private class RegisteredClaims
        {
            internal string[] Aud { get; private set; }

            internal string Exp { get; private set; }

            internal string Iss { get; private set; }

            internal string Nbf { get; private set; }

            internal void SetClaim(string key, string value)
            {
                switch (key)
                {
                    case "aud":
                        this.SetAudiences(value);
                        break;

                    case "exp":
                        this.Exp = value;
                        break;

                    case "iss":
                        this.Iss = value;
                        break;

                    case "nbf":
                        this.Nbf = value;
                        break;
                }
            }

            private void SetAudiences(string aud)
            {
                if (aud.StartsWith("["))
                {
                    using (var parser = new JsonObjectParser(aud))
                    {
                        this.Aud = parser.GetArrayValues().ToArray();
                    }
                }
                else
                {
                    this.Aud = new[] { aud };
                }
            }
        }
    }
}
