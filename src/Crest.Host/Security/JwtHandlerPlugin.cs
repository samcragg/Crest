// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System;
    using System.Net;
    using System.Reflection;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Core;
    using Crest.Host.Logging;

    /// <summary>
    /// Inspects the authentication token and injects it into the pipeline.
    /// </summary>
    internal sealed class JwtHandlerPlugin : IPreRequestPlugin
    {
        private const string AuthorizationHeader = "Authorization";
        private const string BearerPrefix = "Bearer ";

        private static readonly Task<IResponseData> ContinueRequest =
            Task.FromResult<IResponseData>(null);

        private static readonly AsyncLocal<ClaimsPrincipal> CurrentPrincipal =
            new AsyncLocal<ClaimsPrincipal>();

        private static readonly ILog Logger = LogProvider.For<JwtHandlerPlugin>();
        private static readonly Task<IResponseData> UnauthorizedRequest;

        private readonly IScopedServiceRegister serviceRegister;
        private readonly JwtSignatureVerifier signatureVerifier;
        private readonly JwtValidator validator;

        static JwtHandlerPlugin()
        {
            byte[] unauthorizedText = Encoding.UTF8.GetBytes("401 Unauthorized");
            var unauthorizedResponse = new ResponseData(
                "text/plain",
                (int)HttpStatusCode.Unauthorized,
                stream => stream.WriteAsync(unauthorizedText, 0, unauthorizedText.Length));

            unauthorizedResponse.Headers.Add("WWW-Authenticate", BearerPrefix);
            UnauthorizedRequest = Task.FromResult<IResponseData>(unauthorizedResponse);

            ClaimsPrincipal.ClaimsPrincipalSelector = () => CurrentPrincipal.Value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtHandlerPlugin"/> class.
        /// </summary>
        /// <param name="register">Used to register the <see cref="IPrincipal"/>.</param>
        /// <param name="validator">Used to validate the JWT payload.</param>
        /// <param name="signature">Used to validate the JWT signature.</param>
        public JwtHandlerPlugin(IScopedServiceRegister register, JwtValidator validator, JwtSignatureVerifier signature)
        {
            this.serviceRegister = register;
            this.validator = validator;
            this.signatureVerifier = signature;
        }

        /// <inheritdoc />
        public int Order => 1;

        /// <inheritdoc />
        public Task<IResponseData> ProcessAsync(IRequestData request)
        {
            if (this.validator.IsEnabled)
            {
                if (request.Headers.TryGetValue(AuthorizationHeader, out string authorization))
                {
                    if (!this.ValidateBearerToken(authorization, out ClaimsPrincipal principal))
                    {
                        return UnauthorizedRequest;
                    }

                    this.serviceRegister.UseInstance(typeof(IPrincipal), principal);
                }
                else if (!IsAnonymous(request.Handler))
                {
                    Logger.InfoFormat("Access to {method} requires an authorization header", request.Handler.Name);
                    return UnauthorizedRequest;
                }
            }

            return ContinueRequest;
        }

        private static bool IsAnonymous(MethodInfo handler)
        {
            return handler.GetCustomAttribute<AllowAnonymousAttribute>() != null;
        }

        private bool ValidateBearerToken(string authorization, out ClaimsPrincipal principal)
        {
            if (!authorization.StartsWith(BearerPrefix, StringComparison.Ordinal))
            {
                Logger.Warn("Authorization header must start with " + BearerPrefix);
                principal = null;
            }
            else if (!this.signatureVerifier.IsSignatureValid(
                authorization.Substring(BearerPrefix.Length),
                out byte[] payload))
            {
                principal = null;
            }
            else
            {
                principal = this.validator.GetValidClaimsPrincipal(payload);
            }

            if (principal == null)
            {
                return false;
            }

            CurrentPrincipal.Value = principal;
            return true;
        }
    }
}
