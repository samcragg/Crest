namespace BasicExample
{
    using System;
    using System.Collections.Generic;
    using Crest.Abstractions;

    /// <summary>
    /// Shows an example of how to disable JWT authentication.
    /// </summary>
    public sealed class DisableJwtAuthentication : IJwtSettings
    {
        /// <inheritdoc />
        public ISet<string> Audiences { get; }

        /// <inheritdoc />
        public string AuthenticationType { get; }

        /// <inheritdoc />
        public TimeSpan ClockSkew { get; }

        /// <inheritdoc />
        public ISet<string> Issuers { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> JwtClaimMappings { get; }

        /// <inheritdoc />
        public bool SkipAuthentication => true;
    }
}
