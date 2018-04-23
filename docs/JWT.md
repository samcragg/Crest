# JWT Support

Out of the box, the library supports reading and validating JSON Web Tokens that
are sent in the `Authorization` header of the request. By default, all methods
are assumed to require authorization unless you opt out with the
`AllowAnonymousAttribute` on the route.

To send a request to the service, the client must include the following header:

    Authorization: Bearer <token>

where the `<token>` is the actual JWT. A great site to generate this is
[jwt.io](https://jwt.io/)

## Getting The Authorization Information

If the token is successfully parsed and validated, then the
[`ClaimsPrincipal.Current`](https://docs.microsoft.com/en-gb/dotnet/api/system.security.claims.claimsprincipal.current)
will return the information. However, you can also request that an
[`IPrincipal`](https://docs.microsoft.com/en-gb/dotnet/api/system.security.principal.iprincipal)
be injected into your constructor, which is the preferred method as it makes
unit testing of your logic easier:

```C#
public MyLogic : IMyService
{
    public MyLogic(IPrincipal user)
    {
        this.userName = user.Identity.Name;
    }
}
```

## Algorithm Support

The JWT validator supports the following algorithms (taken from
[RFC 7518](https://www.rfc-editor.org/rfc/rfc7518.txt)). Note that `none` is NOT
supported due to security concerns.

| "alg" Param  | Digital Signature or MAC Algorithm |
-----------------------------------------------------
| HS256        | HMAC using SHA-256                 |
| HS384        | HMAC using SHA-384                 |
| HS512        | HMAC using SHA-512                 |
| RS256        | RSASSA-PKCS1-v1_5 using SHA-256    |
| RS384        | RSASSA-PKCS1-v1_5 using SHA-384    |
| RS512        | RSASSA-PKCS1-v1_5 using SHA-512    |
| ES256        | ECDSA using P-256 and SHA-256      |
| ES384        | ECDSA using P-384 and SHA-384      |
| ES512        | ECDSA using P-521 and SHA-512      |

## Configuration

To support the validation of the JWTs, the certificates need to be loaded or the
shared keys needs to be provided, depending on the encoding scheme. To do this,
implement the `ISecurityKeyProvider` interface and return the relevant
information. Note this interface can be implemented multiple times, but the
order they will be invoked in it non-deterministic.

To improve performance, the values returned by the key provider are cached. To
detect when the cache is invalid, the interface provides a `Version` property
that returns an integer. When the cache gets the certificates/secret keys from
the provider it will remember the current value for the version. It will then
only ask for the certificates/keys from that provider again when it returns a
different value from this property.

## Disabling authentication

If you want to disable JWT authentication so that all requests are treated as
anonymous, you can implement the `IJwtSettings` interface and return `true` from
the `SkipAuthentication` property, for example:

```C#
/// <summary>
/// Shows an example of how to disable JWT authentication.
/// </summary>
public sealed class DisableJwtAuthentication : IJwtSettings
{
    public ISet<string> Audiences { get; }
    public string AuthenticationType { get; }
    public TimeSpan ClockSkew { get; }
    public ISet<string> Issuers { get; }
    public IReadOnlyDictionary<string, string> JwtClaimMappings { get; }

    // This is the line to disable authentication
    public bool SkipAuthentication => true;
}
```
