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
builder.Services
    .AddNWebDav(opts => opts.RequireAuthentication = true)
    .AddDiskStore<UserDiskStore>();

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.GetTempPath()));

builder.Services
    .AddAuthentication(opts => opts.DefaultScheme = BasicAuthenticationDefaults.AuthenticationScheme)
    .AddBasicAuthentication(opts =>
    {
        opts.AllowInsecureProtocol = true;
        opts.CacheCookieName = "NWebDAV";
        opts.CacheCookieExpiration = TimeSpan.FromHours(1);
        opts.Events.OnValidateCredentials = context =>
        {
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