using Microsoft.Extensions.DependencyInjection;
using NWebDav.Server;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

public static class Extensions
{
    public static IServiceCollection AddAzureBlob(this IServiceCollection services)
        => services
            .AddStore<AzureBlobStore>()
            .AddSingleton<IAzurePropertyManagerProvider, AzurePropertyManagerProvider>()
            .AddSingleton<AzureContainerCollectionPropertyManager>()
            .AddSingleton<AzureBlobCollectionPropertyManager>()
            .AddSingleton<AzureBlobItemPropertyManager>();
}