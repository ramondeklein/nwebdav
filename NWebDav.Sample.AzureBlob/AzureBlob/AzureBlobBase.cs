using System;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NWebDav.Server.Props;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal abstract class AzureBlobBase
{
    protected AzureBlobBase(AzureBlobStore store, BlobContainerClient blobContainerClient, IPropertyManager propertyManager, IStoreItemMetadata storeItemMetadata)
    {
        StoreItemMetadata = storeItemMetadata;
        Store = store;
        BlobContainerClient = blobContainerClient;
        PropertyManager = propertyManager;
    }

    internal IStoreItemMetadata StoreItemMetadata { get; }
    
    protected AzureBlobStore Store { get; }
    protected BlobContainerClient BlobContainerClient { get; }
    protected string Path => StoreItemMetadata.Path;
    protected BlobClient BlobClient => BlobContainerClient.GetBlobClient(Path);

    public string Name => !string.IsNullOrEmpty(Path) ? System.IO.Path.GetFileName(Path) : BlobContainerClient.Name;
    public string UniqueKey => BlobContainerClient.Name + '/' + Path;

    public IPropertyManager? PropertyManager { get; }
}

internal interface IStoreItemMetadata
{
    string Path { get; }
    bool IsFolder { get; }
    DateTime CreatedOn { get; }
    long ContentLength { get; }
    string ContentType { get; }
    string ETag { get; }

}

// We need this to bridge the difference between BlobProperties and BlobItemProperties 
internal sealed class StoreItemMetadata : IStoreItemMetadata
{
    public static StoreItemMetadata Root { get; } = new();

    private StoreItemMetadata()
    {
        Path = string.Empty;
        IsFolder = true;
        CreatedOn = DateTime.UnixEpoch;
        ContentLength = 0;
        ContentType = string.Empty;
        ETag = string.Empty;
    }
    
    private static bool DetermineIsFolder(IDictionary<string, string> metadata) => metadata.TryGetValue("Folder", out _); 
    
    public StoreItemMetadata(string path, BlobProperties blobProperties)
    {
        Path = path;
        IsFolder = DetermineIsFolder(blobProperties.Metadata);
        CreatedOn = blobProperties.CreatedOn.DateTime;
        ContentLength = blobProperties.ContentLength;
        ContentType = blobProperties.ContentType ?? string.Empty;
        ETag = blobProperties.ETag.ToString();
    }
    
    public StoreItemMetadata(string path, BlobItemProperties blobItemProperties, IDictionary<string, string> metadata)
    {
        Path = path;
        IsFolder = DetermineIsFolder(metadata);
        CreatedOn = blobItemProperties.CreatedOn?.DateTime ?? DateTime.UnixEpoch;
        ContentLength = blobItemProperties.ContentLength ?? 0L;
        ContentType = blobItemProperties.ContentType ?? string.Empty;
        ETag = blobItemProperties.ETag?.ToString() ?? string.Empty;
    }

    public string Path { get; }
    public bool IsFolder { get; }
    public DateTime CreatedOn { get; }
    public long ContentLength { get; }
    public string ContentType { get; }
    public string ETag { get; }
}
