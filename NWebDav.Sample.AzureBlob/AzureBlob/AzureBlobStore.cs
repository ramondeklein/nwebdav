using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using NWebDav.Server.Helpers;
using NWebDav.Server.Stores;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal class AzureBlobStore : IStore
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IAzurePropertyManagerProvider _propertyManagerProvider;

    public AzureBlobStore(BlobServiceClient blobServiceClient, IAzurePropertyManagerProvider propertyManagerProvider)
    {
        _blobServiceClient = blobServiceClient;
        _propertyManagerProvider = propertyManagerProvider;
    }

    public bool IsIsWritable => true;   // TODO
    
    public Task<IStoreItem?> GetItemAsync(Uri uri, CancellationToken cancellationToken)
        => GetItemAsync(GetPathFromUri(uri), cancellationToken);

    public Task<IStoreCollection?> GetCollectionAsync(Uri uri, CancellationToken cancellationToken)
        => GetCollectionAsync(GetPathFromUri(uri), cancellationToken);

    internal async Task<IStoreItem?> GetItemAsync(string path, CancellationToken cancellationToken)
    {
        var (container, relativePath) = SplitPath(path);
        
        if (container == string.Empty)
            return CreateAzureContainerCollection();

        if (!IsValidContainerName(container))
            return null;

        try
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(container);
            if (relativePath == string.Empty)
            {
                if (!await blobContainerClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
                    return null;

                return CreateAzureBlobCollection(blobContainerClient, StoreItemMetadata.Root);
            }

            var blobClient = blobContainerClient.GetBlobClient(relativePath);
            var props = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (props == null) return null;

            var storeItemMetadata = new StoreItemMetadata(blobClient.Name, props);
            return storeItemMetadata.IsFolder ? CreateAzureBlobCollection(blobContainerClient, storeItemMetadata) : CreateAzureBlob(blobContainerClient, storeItemMetadata);
        }
        catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    internal async Task<IStoreCollection?> GetCollectionAsync(string path, CancellationToken cancellationToken)
    {
        var item = await GetItemAsync(path, cancellationToken).ConfigureAwait(false);
        return item as IStoreCollection;
    }

    private (string, string) SplitPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return (string.Empty, string.Empty);
        var containerSeparatorIndex = path.IndexOf('/');
        if (containerSeparatorIndex < 0) return (path, string.Empty);
        return (path[..containerSeparatorIndex], path[(containerSeparatorIndex + 1)..]);
    }

    private AzureContainerCollection CreateAzureContainerCollection() =>
        new(this, _blobServiceClient, _propertyManagerProvider.GetPropertyManager<AzureContainerCollection>());

    internal AzureBlobCollection CreateAzureBlobCollection(BlobContainerClient client, IStoreItemMetadata blob) =>
        new(this, client, _propertyManagerProvider.GetPropertyManager<AzureBlobCollection>(), blob);

    internal AzureBlobItem CreateAzureBlob(BlobContainerClient client, IStoreItemMetadata blob) =>
        new(this, client, _propertyManagerProvider.GetPropertyManager<AzureBlobItem>(), blob);

    private static string GetPathFromUri(Uri uri) => UriHelper.GetDecodedPath(uri)[1..];

    private static bool IsValidContainerName(string containerName)
    {
        var len = containerName.Length;
        
        if (len < 3 || len > 63) return false;
        var hadHyphen = true;
        for (var i = 0; i < len; ++i)
        {
            var ch = containerName[i];
            var isAlphaNum = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9');
            var isHyphen = ch == '-';
            if (isAlphaNum)
            {
                hadHyphen = false;
                continue;
            }

            if (isHyphen && !hadHyphen)
            {
                hadHyphen = true;
                continue;
            }

            return false;
        }

        return true;
    }
}