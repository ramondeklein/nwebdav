using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using NWebDav.Sample.Kestrel;
using NWebDav.Server;
using NWebDav.Server.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add NWebDAV services and set the options 
builder.Services
    .AddNWebDav(opts => opts.RequireAuthentication = true)
    .AddDiskStore<UserDiskStore>();

// Data protection is used to protect cached cookies. If you're
// not using cached cookies, then data protection is not required
// by NWebDAV.
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.GetTempPath()));

builder.Services
    .AddAuthentication(opts => opts.DefaultScheme = BasicAuthenticationDefaults.AuthenticationScheme)
    .AddBasicAuthentication(opts =>
    {
        opts.AllowInsecureProtocol = true;  // This will enable NWebDAV to allow authentication via HTTP, but your client may not allow it
        opts.CacheCookieName = "NWebDAV";   // Cache the authorization result in a cookie
        opts.CacheCookieExpiration = TimeSpan.FromHours(1); // Cached credentials in the cookie are valid for an hour
        opts.Events.OnValidateCredentials = context =>
        {
            // In a real-world application, this is where you would contact
            // you identity provider and validate the credentials and determine
            // the claims.
            if (context is { Username: "test", Password: "nwebdav" })
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                    new Claim(ClaimTypes.Name, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                };

                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                context.Success();
            }
            else
            {
                context.Fail("invalid credentials");
            }

            return Task.CompletedTask;
        };
    });

var app = builder.Build();
app.UseAuthentication();
app.UseNWebDav();

// It this fails, then make sure you have created the certificate. Note that
// the certificate should also be imported in the certificate store of the
// local machine to trust it.
app.Run();