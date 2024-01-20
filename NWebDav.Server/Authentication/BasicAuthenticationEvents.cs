using System;
using System.Threading.Tasks;

namespace NWebDav.Server.Authentication;

public class BasicAuthenticationEvents
{
    /// <summary>
    /// A delegate assigned to this property will be invoked when the
    /// authentication handler fails and encounters an exception.
    /// </summary>
    public Func<BasicAuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = _ => Task.CompletedTask;

    /// <summary>
    /// A delegate assigned to this property will be invoked when the
    /// credentials need validation.
    /// </summary>
    /// <remarks>
    /// You must provide a delegate for this property for authentication to
    /// occur. In your delegate you should construct an authentication
    /// principal from the user details, attach it to the
    /// <see cref="ValidateCredentialsContext.Principal"/> 
    /// property and finally call
    /// <see cref="ValidateCredentialsContext.Success"/>.
    /// </remarks>
    public Func<ValidateCredentialsContext, Task> OnValidateCredentials { get; set; } = _ => Task.CompletedTask;

    public Task AuthenticationFailed(BasicAuthenticationFailedContext context) => OnAuthenticationFailed(context);

    public Task ValidateCredentials(ValidateCredentialsContext context) => OnValidateCredentials(context);
}