using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores
{
    [DebuggerDisplay("{DirectoryInfo.FullPath}\\")]
    public sealed class DiskStoreCollection : IStoreCollection
    {
        private readonly DiskStoreBase _diskStore;
        private readonly ILogger<DiskStoreCollection> _logger;

        public DiskStoreCollection(DiskStoreBase diskStore, DiskStoreCollectionPropertyManager propertyManager, DirectoryInfo directoryInfo, ILogger<DiskStoreCollection> logger)
        {
            _diskStore = diskStore;
            DirectoryInfo = directoryInfo;
            _logger = logger;
            PropertyManager = propertyManager;
        }

        public DirectoryInfo DirectoryInfo { get; }
        public string Name => DirectoryInfo.Name;
        public string UniqueKey => DirectoryInfo.FullName;
        public string FullPath => DirectoryInfo.FullName;
        public bool IsWritable => _diskStore.IsWritable;

        // Disk collections (a.k.a. directories don't have their own data)
        public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken) => Task.FromResult(Stream.Null);
        public Task<DavStatusCode> UploadFromStreamAsync(Stream inputStream, CancellationToken cancellationToken) => Task.FromResult(DavStatusCode.Conflict);

        public IPropertyManager PropertyManager { get; }

        public Task<IStoreItem?> GetItemAsync(string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Determine the full path
            var fullPath = Path.Combine(FullPath, name);

            // Check if the item is a file
            if (File.Exists(fullPath))
                return Task.FromResult<IStoreItem?>(_diskStore.CreateItem(new FileInfo(fullPath)));

            // Check if the item is a directory
            if (Directory.Exists(fullPath))
                return Task.FromResult<IStoreItem?>(_diskStore.CreateCollection(new DirectoryInfo(fullPath)));

            // Item not found
            return Task.FromResult<IStoreItem?>(null);
        }

        public Task<IEnumerable<IStoreItem>> GetItemsAsync(CancellationToken cancellationToken)
        {
            IEnumerable<IStoreItem> GetItemsInternal()
            {
                // Add all directories
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var subDirectory in DirectoryInfo.GetDirectories())
                    yield return _diskStore.CreateCollection(subDirectory);

                // Add all files
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var file in DirectoryInfo.GetFiles())
                    yield return _diskStore.CreateItem(file);
            }

            return Task.FromResult(GetItemsInternal());
        }

        public Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Return error
            if (!IsWritable)
                return Task.FromResult(new StoreItemResult(DavStatusCode.PreconditionFailed));

            // Determine the destination path
            var destinationPath = Path.Combine(FullPath, name);

            // Determine result
            DavStatusCode result;

            // Check if the file can be overwritten
            if (File.Exists(name))
            {
                if (!overwrite)
                    return Task.FromResult(new StoreItemResult(DavStatusCode.PreconditionFailed));

                result = DavStatusCode.NoContent;
            }
            else
            {
                result = DavStatusCode.Created;
            }

            try
            {
                // Create a new file
                File.Create(destinationPath).Dispose();
            }
            catch (Exception exc)
            {
                // Log exception
                _logger.LogError(exc, $"Unable to create '{destinationPath}' file.");
                return Task.FromResult(new StoreItemResult(DavStatusCode.InternalServerError));
            }

            // Return result
            return Task.FromResult(new StoreItemResult(result, _diskStore.CreateItem(new FileInfo(destinationPath))));
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
            return Task.FromResult(new StoreCollectionResult(result, _diskStore.CreateCollection(new DirectoryInfo(destinationPath))));
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
                            return new StoreItemResult(result, _diskStore.CreateItem(new FileInfo(destinationPath)));

                        case DiskStoreCollection _:
                            // Move the directory
                            Directory.Move(sourcePath, destinationPath);
                            return new StoreItemResult(result, _diskStore.CreateCollection(new DirectoryInfo(destinationPath)));

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

        public override int GetHashCode() => DirectoryInfo.FullName.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is DiskStoreCollection storeCollection &&
            storeCollection.DirectoryInfo.FullName.Equals(DirectoryInfo.FullName, StringComparison.CurrentCultureIgnoreCase);
    }
}