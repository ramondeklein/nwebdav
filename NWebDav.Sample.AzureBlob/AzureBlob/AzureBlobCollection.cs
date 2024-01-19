using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NWebDav.Server;
using NWebDav.Server.Props;
using NWebDav.Server.Stores;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal class AzureBlobCollection : AzureBlobBase, IStoreCollection
{
    private static readonly Dictionary<string, string> s_folderMetadata = new()
    {
        {"Folder", "1"}
    };

    public AzureBlobCollection(AzureBlobStore store, BlobContainerClient blobContainerClient, IPropertyManager propertyManager, IStoreItemMetadata storeItemMetadata)
        : base(store, blobContainerClient, propertyManager, storeItemMetadata)
    {
    }

    public bool IsWritable => Store.IsIsWritable;

    public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken) => Task.FromResult(Stream.Null);
    public Task<DavStatusCode> UploadFromStreamAsync(Stream source, CancellationToken cancellationToken)=> Task.FromResult(DavStatusCode.Conflict);

    public async Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken)
    {
        var result = await destination.CreateCollectionAsync(name, overwrite, cancellationToken).ConfigureAwait(false);
        return new StoreItemResult(result.Result, result.Collection);
    }

    public Task<IStoreItem?> GetItemAsync(string name, CancellationToken cancellationToken) 
        => Store.GetItemAsync(BlobContainerClient.Name + '/' + Path + '/' + name, cancellationToken);

    public IAsyncEnumerable<IStoreItem> GetItemsAsync(CancellationToken cancellationToken)
        => GetChildItems(cancellationToken).Select(i => i.IsFolder ? (IStoreItem)Store.CreateAzureBlobCollection(BlobContainerClient, i) : Store.CreateAzureBlob(BlobContainerClient, i));

    public async Task<StoreItemResult> CreateItemAsync(string name, Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(name);
        var result = await blobClient.UploadAsync(stream, overwrite, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new StoreItemResult
        {
            Item = await Store.GetItemAsync(blobClient.Name, cancellationToken).ConfigureAwait(false),
            Result = result != null ? DavStatusCode.Created : DavStatusCode.Conflict
        };
    }

    public async Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(name);
        await blobClient.UploadAsync(Stream.Null, metadata: s_folderMetadata, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new StoreCollectionResult
        {
            Collection = await Store.GetCollectionAsync(blobClient.Name, cancellationToken).ConfigureAwait(false),
            Result = DavStatusCode.Created
        };
    }

    public bool SupportsFastMove(IStoreCollection destination, string destinationName, bool overwrite)
    {
        // TODO
        return false;
    }

    public Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destination, string destinationName, bool overwrite, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public async Task<DavStatusCode> DeleteItemAsync(string name, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(name);
        var result = await blobClient.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result?.IsError ?? true)
            return DavStatusCode.Conflict;
        return DavStatusCode.Ok;
    }

    public InfiniteDepthMode InfiniteDepthMode => InfiniteDepthMode.Rejected;

    public async IAsyncEnumerable<IStoreItemMetadata> GetChildItems([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var prefix = string.IsNullOrEmpty(Path) ? "" : Path + '/';
        var items = BlobContainerClient.GetBlobsByHierarchyAsync(BlobTraits.Metadata, BlobStates.None, "/", prefix: prefix, cancellationToken: cancellationToken);
        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (item.IsBlob)
                yield return new StoreItemMetadata(BlobContainerClient.Name + '/' + item.Blob.Name, item.Blob.Properties, item.Blob.Metadata);
        }
    }
    
    private BlobClient GetBlobClient(string name) => BlobContainerClient.GetBlobClient(Path + '/' + name);
}
