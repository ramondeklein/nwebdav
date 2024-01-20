using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NWebDav.Server.Authentication;
using NWebDav.Server.Handlers;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Server;

public static class Extensions
{
    public static IServiceCollection AddNWebDav(this IServiceCollection services, Action<NWebDavOptions>? configureOptions = null)
    {
        services
            .AddHttpContextAccessor()
            .AddScoped<IXmlReaderWriter, XmlReaderWriter>()
            .AddScoped<CopyHandler>()
            .AddScoped<DeleteHandler>()
            .AddScoped<GetAndHeadHandler>()
            .AddScoped<LockHandler>()
            .AddScoped<MkcolHandler>()
            .AddScoped<MoveHandler>()
            .AddScoped<OptionsHandler>()
            .AddScoped<PropFindHandler>()
            .AddScoped<PropPatchHandler>()
            .AddScoped<PutHandler>()
            .AddScoped<UnlockHandler>()
            .AddSingleton<ILockingManager, InMemoryLockingManager>();

        var optionsBuilder = services
            .AddOptions<NWebDavOptions>()
            .Validate(o => o.Handlers.All(h => h.Key.ToUpperInvariant() == h.Key), "Handler methods should be uppercase");

        var methods = new[] { "COPY", "DELETE", "GET", "HEAD", "MKCOL", "MOVE", "OPTIONS", "PROPFIND", "PROPPATCH", "PUT", "UNLOCK" };
        foreach (var method in methods)
        {
            optionsBuilder
                .Validate(o => o.Handlers.TryGetValue(method, out _), $"No handler for '{method}'")
                .Validate(o => !o.Handlers.TryGetValue(method, out var handlerType) || typeof(IRequestHandler).IsAssignableFrom(handlerType), $"Handler for '{method}' doesn't implement {nameof(IRequestHandler)}");
        }

        services.Configure<NWebDavOptions>(opts =>
        {
            // TODO: Find out if there is a more suitable way of doing this
            opts.Handlers["COPY"] = typeof(CopyHandler);
            opts.Handlers["DELETE"] = typeof(DeleteHandler);
            opts.Handlers["GET"] = typeof(GetAndHeadHandler);
            opts.Handlers["HEAD"] = typeof(GetAndHeadHandler);
            opts.Handlers["LOCK"] = typeof(LockHandler);
            opts.Handlers["MKCOL"] = typeof(MkcolHandler);
            opts.Handlers["MOVE"] = typeof(MoveHandler);
            opts.Handlers["OPTIONS"] = typeof(OptionsHandler);
            opts.Handlers["PROPFIND"] = typeof(PropFindHandler);
            opts.Handlers["PROPPATCH"] = typeof(PropPatchHandler);
            opts.Handlers["PUT"] = typeof(PutHandler);
            opts.Handlers["UNLOCK"] = typeof(UnlockHandler);
            configureOptions?.Invoke(opts);                
        });
        
        return services;
    }

    public static IServiceCollection AddStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(this IServiceCollection services) where TStore : class, IStore
        => services.AddScoped<IStore, TStore>();

    public static IServiceCollection AddDiskStore(this IServiceCollection services, Action<DiskStoreOptions>? configure = null)
        => services
            .Configure<DiskStoreOptions>(opts =>
            {
                opts.BaseDirectory = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE") ?? string.Empty;
                configure?.Invoke(opts);
            })
            .AddDiskStore<DiskStore>();

    public static IServiceCollection AddDiskStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDiskStore>(this IServiceCollection services)
        where TDiskStore : DiskStoreBase
        => services
            .AddSingleton<DiskStoreCollectionPropertyManager>()
            .AddSingleton<DiskStoreItemPropertyManager>()
            .AddStore<TDiskStore>();

    public static IApplicationBuilder UseNWebDav(this IApplicationBuilder app)
    {
        var opts = app.ApplicationServices.GetRequiredService<IOptions<NWebDavOptions>>();
        return app.UseMiddleware<NWebDavMiddleware>(opts);
    }
}

public static class BasicAuthenticationExtensions
{
    public static AuthenticationBuilder AddBasicAuthentication(this AuthenticationBuilder builder)
        => builder.AddBasicAuthentication(BasicAuthenticationDefaults.AuthenticationScheme, null);
    
    public static AuthenticationBuilder AddBasicAuthentication(this AuthenticationBuilder builder, Action<BasicAuthenticationOptions> configureOptions)
        => builder.AddBasicAuthentication(BasicAuthenticationDefaults.AuthenticationScheme, configureOptions);
    
    public static AuthenticationBuilder AddBasicAuthentication(this AuthenticationBuilder builder, string authenticationScheme, Action<BasicAuthenticationOptions>? configureOptions)
        => builder.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(authenticationScheme, configureOptions);
}