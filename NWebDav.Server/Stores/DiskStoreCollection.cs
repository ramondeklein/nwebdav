using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;

using NWebDav.Server.Locking;
using NWebDav.Server.Logging;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores
{
    [DebuggerDisplay("{_directoryInfo.FullPath}\\")]
    public class DiskStoreCollection : IStoreCollection
    {
        private static readonly ILogger s_log = LoggerFactory.CreateLogger(typeof(DiskStoreCollection));
        private readonly DirectoryInfo _directoryInfo;

        public DiskStoreCollection(ILockingManager lockingManager, DirectoryInfo directoryInfo)
        {
            LockingManager = lockingManager;
            _directoryInfo = directoryInfo;
        }

        public static PropertyManager<DiskStoreCollection> DefaultPropertyManager { get; } = new PropertyManager<DiskStoreCollection>(new DavProperty<DiskStoreCollection>[]
        {
                // RFC-2518 properties
                new DavCreationDate<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.CreationTimeUtc,
                    Setter = (principal, collection, value) => { collection._directoryInfo.CreationTimeUtc = value; return DavStatusCode.Ok; }
                },
                new DavDisplayName<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.Name
                },
                new DavGetLastModified<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.LastWriteTimeUtc,
                    Setter = (principal, collection, value) => { collection._directoryInfo.LastWriteTimeUtc = value; return DavStatusCode.Ok; }
                },
                new DavGetResourceType<DiskStoreCollection>
                {
                    Getter = (principal, collection) => new XElement(WebDavNamespaces.DavNs + "collection")
                },

                // Default locking property handling via the LockingManager
                new DavLockDiscoveryDefault<DiskStoreCollection>(),
                new DavSupportedLockDefault<DiskStoreCollection>(),

                // Hopmann/Lippert collection properties
                new DavExtCollectionChildCount<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.EnumerateFiles().Count() + collection._directoryInfo.EnumerateDirectories().Count()
                },
                new DavExtCollectionIsFolder<DiskStoreCollection>
                {
                    Getter = (principal, collection) => true
                },
                new DavExtCollectionIsHidden<DiskStoreCollection>
                {
                    Getter = (principal, collection) => (collection._directoryInfo.Attributes & FileAttributes.Hidden) != 0
                },
                new DavExtCollectionIsStructuredDocument<DiskStoreCollection>
                {
                    Getter = (principal, collection) => false
                },
                new DavExtCollectionHasSubs<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.EnumerateDirectories().Any()
                },
                new DavExtCollectionNoSubs<DiskStoreCollection>
                {
                    Getter = (principal, collection) => false
                },
                new DavExtCollectionObjectCount<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.EnumerateFiles().Count()
                },
                new DavExtCollectionReserved<DiskStoreCollection>
                {
                    Getter = (principal, collection) => false
                },
                new DavExtCollectionVisibleCount<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.EnumerateFiles().Count(fi => (fi.Attributes & FileAttributes.Hidden) == 0)
                },
                
                // Win32 extensions
                new Win32CreationTime<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.CreationTimeUtc,
                    Setter = (principal, collection, value) => { collection._directoryInfo.CreationTimeUtc = value; return DavStatusCode.Ok; }
                },
                new Win32LastAccessTime<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.LastAccessTimeUtc,
                    Setter = (principal, collection, value) => { collection._directoryInfo.LastAccessTimeUtc = value; return DavStatusCode.Ok; }
                },
                new Win32LastModifiedTime<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.LastWriteTimeUtc,
                    Setter = (principal, collection, value) => { collection._directoryInfo.LastWriteTimeUtc = value; return DavStatusCode.Ok; }
                },
                new Win32FileAttributes<DiskStoreCollection>
                {
                    Getter = (principal, collection) => collection._directoryInfo.Attributes,
                    Setter = (principal, collection, value) => { collection._directoryInfo.Attributes = value; return DavStatusCode.Ok; }
                }
        });

        public string Name => _directoryInfo.Name;
        public string FullPath => _directoryInfo.FullName;

        // Disk collections (a.k.a. directories don't have their own data)
        public Stream GetReadableStream(IPrincipal principal) => null;
        public Stream GetWritableStream(IPrincipal principal) => null;

        public IPropertyManager PropertyManager => DefaultPropertyManager;
        public ILockingManager LockingManager { get; }

        public Task<IStoreItem> GetItemAsync(string name, IPrincipal principal)
        {
            // Determine the full path
            var fullPath = Path.Combine(_directoryInfo.FullName, name);

            // Check if the item is a file
            if (File.Exists(fullPath))
                return Task.FromResult<IStoreItem>(new DiskStoreItem(LockingManager, new FileInfo(fullPath)));

            // Check if the item is a directory
            if (Directory.Exists(fullPath))
                return Task.FromResult<IStoreItem>(new DiskStoreCollection(LockingManager, new DirectoryInfo(fullPath)));

            // Item not found
            return Task.FromResult<IStoreItem>(null);
        }

        public Task<IList<IStoreItem>> GetItemsAsync(IPrincipal principal)
        {
            var items = new List<IStoreItem>();

            // Add all directories
            foreach (var subDirectory in _directoryInfo.GetDirectories())
                items.Add(new DiskStoreCollection(LockingManager, subDirectory));

            // Add all files
            foreach (var file in _directoryInfo.GetFiles())
                items.Add(new DiskStoreItem(LockingManager, file));

            // Return the items
            return Task.FromResult<IList<IStoreItem>>(items);
        }

        public Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, IPrincipal principal)
        {
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
                s_log.Log(LogLevel.Error, $"Unable to create '{destinationPath}' file.", exc);
                return Task.FromResult(new StoreItemResult(DavStatusCode.InternalServerError));
            }

            // Return result
            return Task.FromResult(new StoreItemResult(result, new DiskStoreItem(LockingManager, new FileInfo(destinationPath))));
        }

        public Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, IPrincipal principal)
        {
            // Determine the destination path
            var destinationPath = Path.Combine(FullPath, name);

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

            try
            {
                // Attempt to create the directory
                Directory.CreateDirectory(destinationPath);
            }
            catch (Exception exc)
            {
                // Log exception
                s_log.Log(LogLevel.Error, $"Unable to create '{destinationPath}' directory.", exc);
                return null;
            }

            // Return the collection
            return Task.FromResult(new StoreCollectionResult(result, new DiskStoreCollection(LockingManager, new DirectoryInfo(destinationPath))));
        }

        public async Task<StoreItemResult> CopyAsync(IStoreCollection destinationCollection, string name, bool overwrite, IPrincipal principal)
        {
            // Just create the folder itself
            var result = await destinationCollection.CreateCollectionAsync(name, overwrite, principal);
            return new StoreItemResult(result.Result, result.Collection);
        }

        public async Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destinationCollection, string destinationName, bool overwrite, IPrincipal principal)
        {
            // Determine the object that is being moved
            var item = await GetItemAsync(sourceName, principal);
            if (item == null)
                return new StoreItemResult(DavStatusCode.NotFound);

            // Check if the item is actually a file
            var diskStoreItem = item as DiskStoreItem;
            if (diskStoreItem != null)
            {
                // If the destination collection is a directory too, then we can simply move the file
                var destinationDiskStoreCollection = destinationCollection as DiskStoreCollection;
                if (destinationDiskStoreCollection != null)
                {
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
                    else
                    {
                        // The file will be "created"
                        result = DavStatusCode.Created;
                    }

                    // Move the file
                    File.Move(sourcePath, destinationPath);
                    return new StoreItemResult(result, new DiskStoreItem(LockingManager, new FileInfo(destinationPath)));
                }
                else
                {
                    // Attempt to copy the item to the destination collection
                    var result = await item.CopyAsync(destinationCollection, destinationName, overwrite, principal);
                    if (result.Result == DavStatusCode.Created || result.Result == DavStatusCode.NoContent)
                        await DeleteItemAsync(sourceName, principal);

                    // Return the result
                    return result;
                }
            }
            else
            {
                // If it's not a plain item, then it's a collection
                Debug.Assert(item is DiskStoreCollection);

                // Collections will never be moved, but always be created
                // (we always move the individual items to make sure locking is checked properly)
                throw new InvalidOperationException("Collections should never be moved directly.");
            }
        }

        public Task<DavStatusCode> DeleteItemAsync(string name, IPrincipal principal)
        {
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
            catch (Exception exc)
            {
                // Log exception
                s_log.Log(LogLevel.Error, $"Unable to delete '{fullPath}' directory.", exc);
                return Task.FromResult(DavStatusCode.InternalServerError);
            }
        }

        public bool AllowInfiniteDepthProperties => false;

        public override int GetHashCode()
        {
            return _directoryInfo.FullName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var storeCollection = obj as DiskStoreCollection;
            if (storeCollection == null)
                return false;
            return storeCollection._directoryInfo.FullName.Equals(_directoryInfo.FullName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}