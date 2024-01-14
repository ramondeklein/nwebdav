using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores
{
    [DebuggerDisplay("{_directoryInfo.FullPath}\\")]
    public sealed class DiskStoreCollection : IStoreCollection
    {
        private static readonly XElement s_xDavCollection = new(WebDavNamespaces.DavNs + "collection");
        private readonly DiskStore _diskStore;
        private readonly DirectoryInfo _directoryInfo;
        private readonly ILogger<DiskStoreCollection> _logger;

        public DiskStoreCollection(DiskStore diskStore, DirectoryInfo directoryInfo, ILogger<DiskStoreCollection> logger)
        {
            _diskStore = diskStore;
            _directoryInfo = directoryInfo;
            _logger = logger;
        }

        public static PropertyManager<DiskStoreCollection> DefaultPropertyManager { get; } = new(new DavProperty<DiskStoreCollection>[]
        {
            // RFC-2518 properties
            new DavCreationDate<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.CreationTimeUtc,
                Setter = (_, collection, value) =>
                {
                    collection._directoryInfo.CreationTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new DavDisplayName<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.Name
            },
            new DavGetLastModified<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.LastWriteTimeUtc,
                Setter = (_, collection, value) =>
                {
                    collection._directoryInfo.LastWriteTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new DavGetResourceType<DiskStoreCollection>
            {
                Getter = (_, _) => new []{s_xDavCollection}
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<DiskStoreCollection>(),
            new DavSupportedLockDefault<DiskStoreCollection>(),

            // Hopmann/Lippert collection properties
            new DavExtCollectionChildCount<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.EnumerateFiles().Count() + collection._directoryInfo.EnumerateDirectories().Count()
            },
            new DavExtCollectionIsFolder<DiskStoreCollection>
            {
                Getter = (_, _) => true
            },
            new DavExtCollectionIsHidden<DiskStoreCollection>
            {
                Getter = (_, collection) => (collection._directoryInfo.Attributes & FileAttributes.Hidden) != 0
            },
            new DavExtCollectionIsStructuredDocument<DiskStoreCollection>
            {
                Getter = (_, _) => false
            },
            new DavExtCollectionHasSubs<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.EnumerateDirectories().Any()
            },
            new DavExtCollectionNoSubs<DiskStoreCollection>
            {
                Getter = (_, _) => false
            },
            new DavExtCollectionObjectCount<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.EnumerateFiles().Count()
            },
            new DavExtCollectionReserved<DiskStoreCollection>
            {
                Getter = (_, collection) => !collection.IsWritable
            },
            new DavExtCollectionVisibleCount<DiskStoreCollection>
            {
                Getter = (_, collection) =>
                    collection._directoryInfo.EnumerateDirectories().Count(di => (di.Attributes & FileAttributes.Hidden) == 0) +
                    collection._directoryInfo.EnumerateFiles().Count(fi => (fi.Attributes & FileAttributes.Hidden) == 0)
            },

            // Win32 extensions
            new Win32CreationTime<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.CreationTimeUtc,
                Setter = (_, collection, value) =>
                {
                    collection._directoryInfo.CreationTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastAccessTime<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.LastAccessTimeUtc,
                Setter = (_, collection, value) =>
                {
                    collection._directoryInfo.LastAccessTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastModifiedTime<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.LastWriteTimeUtc,
                Setter = (_, collection, value) =>
                {
                    collection._directoryInfo.LastWriteTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32FileAttributes<DiskStoreCollection>
            {
                Getter = (_, collection) => collection._directoryInfo.Attributes,
                Setter = (_, collection, value) =>
                {
                    collection._directoryInfo.Attributes = value;
                    return DavStatusCode.Ok;
                }
            }
        });

        public ILockingManager LockingManager => _diskStore.LockingManager;
        public string Name => _directoryInfo.Name;
        public string UniqueKey => _directoryInfo.FullName;
        public string FullPath => _directoryInfo.FullName;
        public bool IsWritable => _diskStore.IsWritable;

        // Disk collections (a.k.a. directories don't have their own data)
        public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken) => Task.FromResult(Stream.Null);
        public Task<DavStatusCode> UploadFromStreamAsync(Stream inputStream, CancellationToken cancellationToken) => Task.FromResult(DavStatusCode.Conflict);

        public IPropertyManager PropertyManager => DefaultPropertyManager;

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
                foreach (var subDirectory in _directoryInfo.GetDirectories())
                    yield return _diskStore.CreateCollection(subDirectory);

                // Add all files
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var file in _directoryInfo.GetFiles())
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
            var destinationPath = Path.Combine(_directoryInfo.FullName, name);

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
                    var sourcePath = Path.Combine(_directoryInfo.FullName, sourceName);
                    var destinationPath = Path.Combine(destinationDiskStoreCollection._directoryInfo.FullName, destinationName);

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
            var fullPath = Path.Combine(_directoryInfo.FullName, name);
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

        public override int GetHashCode() => _directoryInfo.FullName.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is DiskStoreCollection storeCollection &&
            storeCollection._directoryInfo.FullName.Equals(_directoryInfo.FullName, StringComparison.CurrentCultureIgnoreCase);
    }
}