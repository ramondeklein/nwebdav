using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NWebDav.Server.Handlers;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Server
{
    public static class Extensions
    {
        public static IServiceCollection AddNWebDav(this IServiceCollection services)
        {
            return services
                .AddSingleton<NWebDavMiddleware>()
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
        }

        public static IServiceCollection AddStore<TStore>(this IServiceCollection services) where TStore : class, IStore
        {
            return services.AddScoped<IStore, TStore>();
        }

        public static IServiceCollection AddDiskStore(this IServiceCollection services, Action<DiskStoreOptions>? configure = null)
        {
            return services
                .Configure<DiskStoreOptions>(opts =>
                {
                    opts.BaseDirectory = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE") ?? string.Empty;
                    configure?.Invoke(opts);
                })
                .AddSingleton<DiskStoreCollectionPropertyManager>()
                .AddSingleton<DiskStoreItemPropertyManager>()
                .AddStore<DiskStore>();
        }

        public static IApplicationBuilder UseNWebDav(this IApplicationBuilder app, Action<NWebDavOptions>? configureOptions = null)
        {
            var opts = new NWebDavOptions
            {
                Handlers =
                {
                    { "COPY", typeof(CopyHandler) },
                    { "DELETE", typeof(DeleteHandler) },
                    { "GET", typeof(GetAndHeadHandler) },
                    { "HEAD", typeof(GetAndHeadHandler) },
                    { "LOCK", typeof(LockHandler) },
                    { "MKCOL", typeof(MkcolHandler) },
                    { "MOVE", typeof(MoveHandler) },
                    { "OPTIONS", typeof(OptionsHandler) },
                    { "PROPFIND", typeof(PropFindHandler) },
                    { "PROPPATCH", typeof(PropPatchHandler) },
                    { "PUT", typeof(PutHandler) },
                    { "UNLOCK", typeof(UnlockHandler) }
                }
            };
            configureOptions?.Invoke(opts);
            foreach (var kv in opts.Handlers)
            {
                if (!kv.Key.ToUpperInvariant().Equals(kv.Key, StringComparison.Ordinal)) throw new InvalidOperationException($"HTTP method '{kv.Key}' should be in uppercase");
                if (!typeof(IRequestHandler).IsAssignableFrom(kv.Value)) throw new InvalidOperationException($"HTTP method '{kv.Key}' handler doesn't implement {nameof(IRequestHandler)}");
            }

            return app
                .UseMiddleware<NWebDavMiddleware>(Options.Create(opts));
        }
    }
}