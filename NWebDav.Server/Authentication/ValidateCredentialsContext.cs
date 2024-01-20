using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace NWebDav.Server.Authentication;

public class ValidateCredentialsContext : ResultContext<BasicAuthenticationOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="ValidateCredentialsContext"/>.
    /// </summary>
    /// <param name="context">
    /// The HttpContext the validate context applies too.
    /// </param>
    /// <param name="scheme">
    /// The scheme used when the Basic authentication handler was registered.
    /// </param>
    /// <param name="options">
    /// The <see cref="BasicAuthenticationOptions"/> for the instance of
    /// <see cref="BasicAuthenticationHandler"/> creating this instance.
    /// </param>
    public ValidateCredentialsContext(HttpContext context, AuthenticationScheme scheme, BasicAuthenticationOptions options)
        : base(context, scheme, options)
    {
    }

    public required string Username { get; init; }
    public required string Password { get; init; }
}