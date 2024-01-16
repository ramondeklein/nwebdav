using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NWebDav.Server.Authentication;

public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
{
    private readonly ILogger<BasicAuthenticationHandler> _logger;

    public BasicAuthenticationHandler(IOptionsMonitor<BasicAuthenticationOptions> options, ILoggerFactory loggerFactory, UrlEncoder encoder, ISystemClock clock) : base(options, loggerFactory, encoder, clock)
    {
        _logger = loggerFactory.CreateLogger<BasicAuthenticationHandler>();
    }
    
    protected new BasicAuthenticationEvents Events
    {
        get => (BasicAuthenticationEvents)base.Events;
        set => base.Events = value;
    }

    protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new BasicAuthenticationEvents());

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps && !Options.AllowInsecureProtocol)
        {
            _logger.LogTrace("Basic authentication is disabled, because insecure protocol is used and not allowed");
            return AuthenticateResult.NoResult();
        }

        var cachedCredential = TryCachedCredential();
        if (cachedCredential != null)
            return AuthenticateResult.Success(new AuthenticationTicket(cachedCredential, Scheme.Name));

        foreach (var authHeader in Request.Headers.Authorization)
        {
            if (!AuthenticationHeaderValue.TryParse(authHeader, out var auth)) continue;
            if (auth.Scheme != "Basic") continue;
            if (!ParseBasicAuthenticationValue(auth.Parameter, out var username, out var password)) continue;
            try
            {
                var validateCredentialsContext = new ValidateCredentialsContext(Context, Scheme, Options)
                {
                    Username = username,
                    Password = password
                };
                await Events.ValidateCredentials(validateCredentialsContext).ConfigureAwait(false);

                if (validateCredentialsContext.Result.Succeeded)
                {
                    _logger.LogTrace("Authenticated user '{Username}'", username);
                    CacheCredentials(validateCredentialsContext.Principal!);
                    return AuthenticateResult.Success(validateCredentialsContext.Result.Ticket);
                }

                var exc = validateCredentialsContext.Result.Failure; 
                if (exc != null)
                {
                    _logger.LogTrace(exc, "Failed to authenticate user '{Username}'", username);
                    return AuthenticateResult.Fail(exc);
                }
            }
            catch (Exception exc)
            {
                _logger.LogTrace(exc, "Failed to authenticate user '{Username}'", username);
                
                var authenticationFailedContext = new BasicAuthenticationFailedContext(Context, Scheme, Options)
                {
                    Exception = exc
                };

                await Events.AuthenticationFailed(authenticationFailedContext).ConfigureAwait(false);
                return authenticationFailedContext.Result;
            }
        }

        return AuthenticateResult.NoResult();
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (!Request.IsHttps && !Options.AllowInsecureProtocol)
        {
            Response.StatusCode = (int)HttpStatusCode.MisdirectedRequest;
            var httpResponseFeature = Response.HttpContext.Features.Get<IHttpResponseFeature>();
            if (httpResponseFeature != null)
                httpResponseFeature.ReasonPhrase = "Not challenging basic authentication via insecure protocol";
        }
        else
        {
            Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            if (!Options.SuppressWwwAuthenticateHeader)
                Response.Headers.WWWAuthenticate = $"Basic realm=\"{Options.Realm}\"";
        }
        return Task.CompletedTask;
    }

    private bool ParseBasicAuthenticationValue(string? authValue, out string username, out string password)
    {
        if (!string.IsNullOrEmpty(authValue))
        {
            try
            {
                var nameAndPassword = Encoding.UTF8.GetString(Convert.FromBase64String(authValue));
                var separatorIndex = nameAndPassword.IndexOf(':');
                if (separatorIndex > 0)
                {
                    username = nameAndPassword[..separatorIndex];
                    password = nameAndPassword[(separatorIndex+1)..];
                    return true;
                }
            }
            catch
            {
                // ignore
            }
        }

        username = password = string.Empty;
        return false;
    }

    private ClaimsPrincipal? TryCachedCredential()
    {
        if (string.IsNullOrEmpty(Options.CacheCookieName) || Options.CacheCookieExpiration <= TimeSpan.Zero)
        {
            _logger.LogTrace("Cookie caching has been disabled in the options");
            return null;
        }

        if (!Request.Cookies.TryGetValue(Options.CacheCookieName, out var cachedCookieValue))
        {
            _logger.LogTrace("Cache Cookie '{CookieName}' not in request", Options.CacheCookieName);
            return null;
        }
        
        try
        {
            var unprotectedBytes = DataProtector.Unprotect(Convert.FromBase64String(cachedCookieValue));
            using var ms = new MemoryStream(unprotectedBytes, false);
            using var br = new BinaryReader(ms);
            var expiration = DateTimeOffset.FromUnixTimeSeconds(br.ReadInt64());
            if (DateTimeOffset.UtcNow < expiration)
            {
                _logger.LogTrace("Using cached credentials from cookie '{CookieName}' (expires at {Expiration})", Options.CacheCookieName, expiration);
                return new ClaimsPrincipal(br);
            }

            _logger.LogTrace("Cached credentials from cookie '{CookieName}' expired at {Expiration}", Options.CacheCookieName, expiration);
        }
        catch (Exception exc)
        {
            _logger.LogTrace(exc, "Parsing cached credentials from cookie '{CookieName}' raised an exception.", Options.CacheCookieName);
        }

        return null;
    }

    private void CacheCredentials(ClaimsPrincipal principal)
    {
        if (string.IsNullOrEmpty(Options.CacheCookieName) || Options.CacheCookieExpiration <= TimeSpan.Zero)
        {
            _logger.LogTrace("Not caching cookie, because it has been disable in the options");
            return;
        }

        var expiration = DateTimeOffset.UtcNow.Add(Options.CacheCookieExpiration);
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(expiration.ToUnixTimeSeconds());
            principal.WriteTo(bw);
        }

        var cookieValue = Convert.ToBase64String(DataProtector.Protect(ms.ToArray()));
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps || !Options.AllowInsecureProtocol,
            IsEssential = true,
            Expires = expiration
        };
        Response.Cookies.Append(Options.CacheCookieName, cookieValue, cookieOptions);

        _logger.LogTrace("Saving cached credentials into cookie '{CookieName}' and expires at {Expiration}", Options.CacheCookieName, cookieOptions.Expires);
    }

    private IDataProtector DataProtector
    {
        get
        {
            var dataProtectionProvider = Context.RequestServices.GetRequiredService<IDataProtectionProvider>();
            return dataProtectionProvider.CreateProtector(Options.DataProtectorName);
        }
    }
}