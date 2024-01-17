using System;
using Microsoft.AspNetCore.Authentication;

namespace NWebDav.Server.Authentication;

public class BasicAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public BasicAuthenticationOptions()
    {
        Events = new BasicAuthenticationEvents();
    }

    /// <summary>
    /// Suppress sending the <c>WWW-Authenticate</c> during a challenge. 
    /// </summary>
    public bool SuppressChallenge { get; set; }
    
    /// <summary>
    /// The Realm is offered to the client, when the basic authentication
    /// issues a challenge.
    /// </summary>
    /// <remarks>
    /// When <seealso cref="SuppressChallenge"/> is set, then no
    /// challenge will be issued.
    /// </remarks>
    public string Realm { get; set; } = "WebDAV";
    
    /// <summary>
    /// Allow using basic authentication via an insecure protocol. The default
    /// is not to allow basic authentication via insecure protocols, because it
    /// will transmit credentials using an unencrypted channel.
    /// </summary>
    /// <remarks>
    /// Although NWebDAV may allow authentication via an insecure protocol, most
    /// WebDAV clients don't support it, because it may expose credentials.
    /// </remarks>
    public bool AllowInsecureProtocol { get; set; }
    
    /// <summary>
    /// CacheCookieName specifies the name of the cookie that is used to store
    /// the claims of an authenticated user. This will cache the credentials
    /// via a protected cookie and prevents username/password lookups for each
    /// request.
    /// </summary>
    public string CacheCookieName { get; set; } = string.Empty;
    
    /// <summary>
    /// CacheCookieExpiration specifies the expiration of the cached claims.
    /// Both the cookie and its value will expire after this duration. The
    /// default value is 5 minutes. Setting this value to
    /// <see cref="TimeSpan.Zero"/> (or a negative value) will disable caching.
    /// </summary>
    /// <remarks>
    /// Note that all claims are cached during this period, so the identity
    /// provider won't be checked (unless done explicitly from handlers and/or
    /// the stores).
    /// </remarks>
    public TimeSpan CacheCookieExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Name of the data-protector that is used to protect the cached cookie
    /// value.
    /// </summary>
    /// <remarks>
    /// There is no typically no reason to change this name, so it's best to
    /// leave it to the default value.
    /// </remarks>
    public string DataProtectorName { get; set; } = "NWebDAV";
    
    /// <summary>
    /// Gets or sets the <see cref="BasicAuthenticationEvents"/> used to
    /// handle authentication events.
    /// </summary>
    public new BasicAuthenticationEvents Events
    {
        get => (BasicAuthenticationEvents)base.Events!;
        set => base.Events = value;
    }
}