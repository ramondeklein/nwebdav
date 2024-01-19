using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NWebDav.Server;
using NWebDav.Server.Props;
using NWebDav.Server.Stores;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal class AzureContainerCollection : IStoreCollection
{
    private readonly AzureBlobStore _store;
    private readonly BlobServiceClient _blobServiceClient;

    public AzureContainerCollection(AzureBlobStore store, BlobServiceClient blobServiceClient, IPropertyManager propertyManager)
    {
        _store = store;
        _blobServiceClient = blobServiceClient;
        PropertyManager = propertyManager;
    }

    public string Name => string.Empty;
    public string UniqueKey => "$container-root$";

    public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken) => Task.FromResult(Stream.Null);
    public Task<DavStatusCode> UploadFromStreamAsync(Stream source, CancellationToken cancellationToken) => Task.FromResult(DavStatusCode.Forbidden);
    public Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken) => Task.FromResult(new StoreItemResult(DavStatusCode.Forbidden));

    public IPropertyManager? PropertyManager { get; }

    public async Task<IStoreItem?> GetItemAsync(string name, CancellationToken cancellationToken)
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(Name);
        try
        {
            await blobContainerClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            return _store.CreateAzureBlobCollection(blobContainerClient, StoreItemMetadata.Root);
        }
        catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async IAsyncEnumerable<IStoreItem> GetItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var blobContainer in GetChildItems(cancellationToken).ConfigureAwait(false))
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobContainer.Name);
            yield return _store.CreateAzureBlobCollection(blobContainerClient, StoreItemMetadata.Root);
        }
    }

    public Task<StoreItemResult> CreateItemAsync(string name, Stream stream, bool overwrite, CancellationToken cancellationToken) => Task.FromResult(new StoreItemResult(DavStatusCode.Forbidden));
    public Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, CancellationToken cancellationToken) => Task.FromResult(new StoreCollectionResult(DavStatusCode.Forbidden));
    public bool SupportsFastMove(IStoreCollection destination, string destinationName, bool overwrite) => false;
    public Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destination, string destinationName, bool overwrite, CancellationToken cancellationToken) => Task.FromResult(new StoreItemResult(DavStatusCode.Forbidden));
    public Task<DavStatusCode> DeleteItemAsync(string name, CancellationToken cancellationToken) => Task.FromResult(DavStatusCode.Forbidden);
    public InfiniteDepthMode InfiniteDepthMode => InfiniteDepthMode.Rejected;

    public async IAsyncEnumerable<BlobContainerItem> GetChildItems([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var blobContainers = _blobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await foreach (var blobContainer in blobContainers)
        {
            if (blobContainer.IsDeleted == null || !blobContainer.IsDeleted.Value)
                yield return blobContainer;
        }
    }
}