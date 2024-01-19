using NWebDav.Server.Props;
using NWebDav.Server.Stores;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal interface IAzurePropertyManagerProvider
{
    IPropertyManager GetPropertyManager<T>() where T : IStoreItem;
}