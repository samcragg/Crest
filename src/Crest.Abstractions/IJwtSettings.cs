// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents configuration options used during the validation of JWTs.
    /// </summary>
    public interface IJwtSettings
    {
        /// <summary>
        /// Gets a list of valid intended recipients.
        /// </summary>
        /// <remarks>
        /// If the <c>aud</c> claim is present in the JWT then this list will
        /// be checked to see if the claim matches one of the values. If no
        /// matches are found then the data in the JWT will be disregarded.
        /// </remarks>
        ISet<string> Audiences { get; }

        /// <summary>
        /// Gets the value to report for the type of authentication used when
        /// creating the claims identity.
        /// </summary>
        string AuthenticationType { get; }

        /// <summary>
        /// Gets the clock skew to apply when validating a time.
        /// </summary>
        TimeSpan ClockSkew { get; }

        /// <summary>
        /// Gets a list of valid issuing parties.
        /// </summary>
        /// <remarks>
        /// If the <c>iss</c> claim is present in the JWT then this list will
        /// be checked to see if the claim matches one of the values. If no
        /// matches are found then the data in the JWT will be disregarded.
        /// </remarks>
        ISet<string> Issuers { get; }

        /// <summary>
        /// Gets the mappings between a JWT and a <c>System.Security.Claims.Claim</c>.
        /// </summary>
        IReadOnlyDictionary<string, string> JwtClaimMappings { get; }

        /// <summary>
        /// Gets a value indicating whether JWT authentication should be
        /// performed or not.
        /// </summary>
        /// <remarks>
        /// This property is provided to allow for easier debugging of the API
        /// without needing to provide a JWT in each request. Returning
        /// <c>true</c> from this property will skip all validation of any
        /// authentication header and will also prevent enforcement that the
        /// header must be specified for non-anonymous methods.
        /// </remarks>
        bool SkipAuthentication { get; }
    }
}
