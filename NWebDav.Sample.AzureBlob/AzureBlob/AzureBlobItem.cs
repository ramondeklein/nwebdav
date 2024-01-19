using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using NWebDav.Server;
using NWebDav.Server.Props;
using NWebDav.Server.Stores;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal class AzureBlobItem : AzureBlobBase, IStoreItem
{
    public AzureBlobItem(AzureBlobStore store, BlobContainerClient blobContainerClient, IPropertyManager propertyManager, IStoreItemMetadata storeItemMetadata)
        : base(store, blobContainerClient, propertyManager, storeItemMetadata)
    {
    }

    public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken)
        => BlobClient.OpenReadAsync(cancellationToken: cancellationToken);

    public async Task<DavStatusCode> UploadFromStreamAsync(Stream source, CancellationToken cancellationToken)
    {
        var result = await BlobClient.UploadAsync(source, cancellationToken).ConfigureAwait(false);
        return result.HasValue ? DavStatusCode.Created : DavStatusCode.Conflict;
    }

    public async Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken)
    {
        var source = await GetReadableStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            var createResult = await destination.CreateItemAsync(name, source, overwrite, cancellationToken).ConfigureAwait(false);
            return createResult;
        }
    }
}