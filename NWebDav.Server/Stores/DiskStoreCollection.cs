using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores;

[DebuggerDisplay("{DirectoryInfo.FullPath}\\")]
public sealed class DiskStoreCollection : IStoreCollection
{
    private readonly DiskStoreBase _store;
    private readonly ILogger<DiskStoreCollection> _logger;

    public DiskStoreCollection(DiskStoreBase store, DiskStoreCollectionPropertyManager propertyManager, DirectoryInfo directoryInfo, ILogger<DiskStoreCollection> logger)
    {
        _store = store;
        DirectoryInfo = directoryInfo;
        _logger = logger;
        PropertyManager = propertyManager;
    }

    public DirectoryInfo DirectoryInfo { get; }
    public string Name => DirectoryInfo.Name;
    public string UniqueKey => DirectoryInfo.FullName;
    public string FullPath => DirectoryInfo.FullName;
    public bool IsWritable => _store.IsWritable;

    // Disk collections (a.k.a. directories don't have their own data)
    public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken) => Task.FromResult(Stream.Null);
    public Task<DavStatusCode> UploadFromStreamAsync(Stream inputStream, CancellationToken cancellationToken) => Task.FromResult(DavStatusCode.Conflict);

    public IPropertyManager PropertyManager { get; }

    public Task<IStoreItem?> GetItemAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
            
        var fullPath = Path.Combine(FullPath, name);
        return Task.FromResult(_store.CreateFromPath(fullPath));
    }

    // Not async, but this is the easiest way to return an IAsyncEnumerable
    public async IAsyncEnumerable<IStoreItem> GetItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Add all directories
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var subDirectory in DirectoryInfo.GetDirectories())
            yield return _store.CreateCollection(subDirectory);

        // Add all files
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var file in DirectoryInfo.GetFiles())
            yield return _store.CreateItem(file);
    }

    public async Task<StoreItemResult> CreateItemAsync(string name, Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
            
        // Return error
        if (!IsWritable)
            return new StoreItemResult(DavStatusCode.PreconditionFailed);

        // Determine the destination path
        var destinationPath = Path.Combine(FullPath, name);

        // Check if the file can be overwritten
        if (File.Exists(name) && !overwrite)
            return new StoreItemResult(DavStatusCode.PreconditionFailed);

        try
        {
            var file = File.Create(destinationPath);
            await using (file.ConfigureAwait(false))
            {
                await stream.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exc)
        {
            // Log exception
            _logger.LogError(exc, "Unable to create '{Path}' file.", destinationPath);
            return new StoreItemResult(DavStatusCode.InternalServerError);
        }

        // Return result
        var item = _store.CreateItem(new FileInfo(destinationPath));
        return new StoreItemResult(DavStatusCode.Created, item);
    }

    public Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
            
        // Return error
        if (!IsWritable)
            return Task.FromResult(new StoreCollectionResult(DavStatusCode.PreconditionFailed));

        // Determine the destination path
        var destinationPath = Path.Combine(DirectoryInfo.FullName, name);

        // Check if the directory can be overwritten
        DavStatusCode result;
        if (Directory.Exists(destinationPath))
        {
            // Check if overwrite is allowed
            if (!overwrite)
                return Task.FromResult(new StoreCollectionResult(DavStatusCode.PreconditionFailed));

            // Overwrite existing
            result = DavStatusCode.NoContent;
        }
        else
        {
            // Created new directory
            result = DavStatusCode.Created;
        }

        // Attempt to create the directory
        Directory.CreateDirectory(destinationPath);

        // Return the collection
        return Task.FromResult(new StoreCollectionResult(result, _store.CreateCollection(new DirectoryInfo(destinationPath))));
    }

    public async Task<StoreItemResult> CopyAsync(IStoreCollection destinationCollection, string name, bool overwrite, CancellationToken cancellationToken)
    {
        // Just create the folder itself
        var result = await destinationCollection.CreateCollectionAsync(name, overwrite, cancellationToken).ConfigureAwait(false);
        return new StoreItemResult(result.Result, result.Collection);
    }

    public bool SupportsFastMove(IStoreCollection destination, string destinationName, bool overwrite)
    {
        // We can only move disk-store collections
        return destination is DiskStoreCollection;
    }

    public async Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destinationCollection, string destinationName, bool overwrite, CancellationToken cancellationToken)
    {
        // Return error
        if (!IsWritable)
            return new StoreItemResult(DavStatusCode.PreconditionFailed);

        // Determine the object that is being moved
        var item = await GetItemAsync(sourceName, cancellationToken).ConfigureAwait(false);
        if (item == null)
            return new StoreItemResult(DavStatusCode.NotFound);

        try
        {
            // If the destination collection is a directory too, then we can simply move the file
            if (destinationCollection is DiskStoreCollection destinationDiskStoreCollection)
            {
                // Return error
                if (!destinationDiskStoreCollection.IsWritable)
                    return new StoreItemResult(DavStatusCode.PreconditionFailed);

                // Determine source and destination paths
                var sourcePath = Path.Combine(DirectoryInfo.FullName, sourceName);
                var destinationPath = Path.Combine(destinationDiskStoreCollection.DirectoryInfo.FullName, destinationName);

                // Check if the file already exists
                DavStatusCode result;
                if (File.Exists(destinationPath))
                {
                    // Remove the file if it already exists (if allowed)
                    if (!overwrite)
                        return new StoreItemResult(DavStatusCode.Forbidden);

                    // The file will be overwritten
                    File.Delete(destinationPath);
                    result = DavStatusCode.NoContent;
                }
                else if (Directory.Exists(destinationPath))
                {
                    // Remove the directory if it already exists (if allowed)
                    if (!overwrite)
                        return new StoreItemResult(DavStatusCode.Forbidden);

                    // The file will be overwritten
                    Directory.Delete(destinationPath, true);
                    result = DavStatusCode.NoContent;
                }
                else
                {
                    // The file will be "created"
                    result = DavStatusCode.Created;
                }

                switch (item)
                {
                    case DiskStoreItem _:
                        // Move the file
                        File.Move(sourcePath, destinationPath);
                        return new StoreItemResult(result, _store.CreateItem(new FileInfo(destinationPath)));

                    case DiskStoreCollection _:
                        // Move the directory
                        Directory.Move(sourcePath, destinationPath);
                        return new StoreItemResult(result, _store.CreateCollection(new DirectoryInfo(destinationPath)));

                    default:
                        // Invalid item
                        Debug.Fail($"Invalid item {item.GetType()} inside the {nameof(DiskStoreCollection)}.");
                        return new StoreItemResult(DavStatusCode.InternalServerError);
                }
            }
            else
            {
                // Attempt to copy the item to the destination collection
                var result = await item.CopyAsync(destinationCollection, destinationName, overwrite, cancellationToken).ConfigureAwait(false);
                if (result.Result == DavStatusCode.Created || result.Result == DavStatusCode.NoContent)
                    await DeleteItemAsync(sourceName, cancellationToken).ConfigureAwait(false);

                // Return the result
                return result;
            }
        }
        catch (UnauthorizedAccessException)
        {
            return new StoreItemResult(DavStatusCode.Forbidden);
        }
    }

    public Task<DavStatusCode> DeleteItemAsync(string name, CancellationToken cancellationToken)
    {
        // Return error
        if (!IsWritable)
            return Task.FromResult(DavStatusCode.PreconditionFailed);

        // Determine the full path
        var fullPath = Path.Combine(DirectoryInfo.FullName, name);
        try
        {
            // Check if the file exists
            if (File.Exists(fullPath))
            {
                // Delete the file
                File.Delete(fullPath);
                return Task.FromResult(DavStatusCode.Ok);
            }

            // Check if the directory exists
            if (Directory.Exists(fullPath))
            {
                // Delete the directory
                Directory.Delete(fullPath);
                return Task.FromResult(DavStatusCode.Ok);
            }

            // Item not found
            return Task.FromResult(DavStatusCode.NotFound);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(DavStatusCode.Forbidden);
        }
        catch (Exception exc)
        {
            // Log exception
            _logger.LogError(exc, "Unable to delete '{Path}' directory.", fullPath);
            return Task.FromResult(DavStatusCode.InternalServerError);
        }
    }

    public InfiniteDepthMode InfiniteDepthMode => InfiniteDepthMode.Rejected;
}