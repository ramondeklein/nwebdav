using System;
using Microsoft.Extensions.DependencyInjection;
using NWebDav.Server.Props;
using NWebDav.Server.Stores;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal class AzurePropertyManagerProvider : IAzurePropertyManagerProvider 
{
    private readonly IServiceProvider _serviceProvider;

    public AzurePropertyManagerProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPropertyManager GetPropertyManager<T>() where T : IStoreItem
    {
        var t = typeof(T);
        if (t == typeof(AzureContainerCollection)) return _serviceProvider.GetRequiredService<AzureContainerCollectionPropertyManager>();
        if (t == typeof(AzureBlobCollection)) return _serviceProvider.GetRequiredService<AzureBlobCollectionPropertyManager>();
        if (t == typeof(AzureBlobItem)) return _serviceProvider.GetRequiredService<AzureBlobItemPropertyManager>();
        throw new InvalidOperationException($"Unknown type '{t}'");
    }
}