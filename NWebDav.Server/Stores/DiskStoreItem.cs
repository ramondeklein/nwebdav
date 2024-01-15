using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores
{
    public class DiskStoreItemPropertyManager : PropertyManager<DiskStoreItem>
    {
        public DiskStoreItemPropertyManager(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) : base(GetProperties(httpContextAccessor, lockingManager))
        {
        }

        private static DavProperty<DiskStoreItem>[] GetProperties(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) => new DavProperty<DiskStoreItem>[]
        {
            // RFC-2518 properties
            new DavCreationDate<DiskStoreItem>(httpContextAccessor)
            {
                Getter = item => item.FileInfo.CreationTimeUtc,
                Setter = (item, value) =>
                {
                    item.FileInfo.CreationTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new DavDisplayName<DiskStoreItem>
            {
                Getter = item => item.FileInfo.Name
            },
            new DavGetContentLength<DiskStoreItem>
            {
                Getter = item => item.FileInfo.Length
            },
            new DavGetContentType<DiskStoreItem>
            {
                Getter = item => MimeTypeHelper.GetMimeType(item.FileInfo.Name)
            },
            new DavGetEtag<DiskStoreItem>
            {
                // Calculating the Etag is an expensive operation,
                // because we need to scan the entire file.
                IsExpensive = true,
                GetterAsync = async (item, ct) =>
                {
                    await using (var stream = File.OpenRead(item.FileInfo.FullName))
                    {
                        var hash = await SHA256.Create().ComputeHashAsync(stream, ct).ConfigureAwait(false);
                        return BitConverter.ToString(hash).Replace("-", string.Empty);
                    }

                }
            },
            new DavGetLastModified<DiskStoreItem>
            {
                Getter = item => item.FileInfo.LastWriteTimeUtc,
                Setter = (item, value) =>
                {
                    item.FileInfo.LastWriteTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new DavGetResourceType<DiskStoreItem>
            {
                Getter = _ => null
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<DiskStoreItem>(lockingManager),
            new DavSupportedLockDefault<DiskStoreItem>(lockingManager),

            // Hopmann/Lippert collection properties
            // (although not a collection, the IsHidden property might be valuable)
            new DavExtCollectionIsHidden<DiskStoreItem>
            {
                Getter = item => (item.FileInfo.Attributes & FileAttributes.Hidden) != 0
            },

            // Win32 extensions
            new Win32CreationTime<DiskStoreItem>
            {
                Getter = item => item.FileInfo.CreationTimeUtc,
                Setter = (item, value) =>
                {
                    item.FileInfo.CreationTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastAccessTime<DiskStoreItem>
            {
                Getter = item => item.FileInfo.LastAccessTimeUtc,
                Setter = (item, value) =>
                {
                    item.FileInfo.LastAccessTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastModifiedTime<DiskStoreItem>
            {
                Getter = item => item.FileInfo.LastWriteTimeUtc,
                Setter = (item, value) =>
                {
                    item.FileInfo.LastWriteTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32FileAttributes<DiskStoreItem>
            {
                Getter = item => item.FileInfo.Attributes,
                Setter = (item, value) =>
                {
                    item.FileInfo.Attributes = value;
                    return DavStatusCode.Ok;
                }
            }
        };
    }

    [DebuggerDisplay("{FileInfo.FullPath}")]
    public sealed class DiskStoreItem : IStoreItem
    {
        private readonly DiskStore _diskStore;
        private readonly ILogger<DiskStoreItem> _logger;

        public DiskStoreItem(DiskStore diskStore, DiskStoreItemPropertyManager propertyManager, FileInfo fileInfo, ILogger<DiskStoreItem> logger)
        {
            _diskStore = diskStore;
            FileInfo = fileInfo;
            PropertyManager = propertyManager;
            _logger = logger;
        }

        public IPropertyManager PropertyManager { get; }

        public FileInfo FileInfo { get; }
        public bool IsWritable => _diskStore.IsWritable;
        public string Name => FileInfo.Name;
        public string UniqueKey => FileInfo.FullName;
        public string FullPath => FileInfo.FullName;
        public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken) => Task.FromResult((Stream)FileInfo.OpenRead());

        public async Task<DavStatusCode> UploadFromStreamAsync(Stream inputStream, CancellationToken cancellationToken)
        {
            // Check if the item is writable
            if (!IsWritable)
                return DavStatusCode.Conflict;

            // Copy the stream
            try
            {
                // Copy the information to the destination stream
                await using (var outputStream = FileInfo.OpenWrite())
                {
                    await inputStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
                }
                return DavStatusCode.Ok;
            }
            catch (IOException ioException) when (ioException.IsDiskFull())
            {
                return DavStatusCode.InsufficientStorage;
            }
        }

        public async Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken)
        {
            try
            {
                // If the destination is also a disk-store, then we can use the FileCopy API
                // (it's probably a bit more efficient than copying in C#)
                if (destination is DiskStoreCollection diskCollection)
                {
                    // Check if the collection is writable
                    if (!diskCollection.IsWritable)
                        return new StoreItemResult(DavStatusCode.PreconditionFailed);

                    var destinationPath = Path.Combine(diskCollection.FullPath, name);

                    // Check if the file already exists
                    var fileExists = File.Exists(destinationPath);
                    if (fileExists && !overwrite)
                        return new StoreItemResult(DavStatusCode.PreconditionFailed);

                    // Copy the file
                    File.Copy(FileInfo.FullName, destinationPath, true);

                    // Return the appropriate status
                    return new StoreItemResult(fileExists ? DavStatusCode.NoContent : DavStatusCode.Created);
                }
                else
                {
                    // Create the item in the destination collection
                    var result = await destination.CreateItemAsync(name, overwrite, cancellationToken).ConfigureAwait(false);

                    // Check if the item could be created
                    if (result.Item != null)
                    {
                        await using (var sourceStream = await GetReadableStreamAsync(cancellationToken).ConfigureAwait(false))
                        {
                            var copyResult = await result.Item.UploadFromStreamAsync(sourceStream, cancellationToken).ConfigureAwait(false);
                            if (copyResult != DavStatusCode.Ok)
                                return new StoreItemResult(copyResult, result.Item);
                        }
                    }

                    // Return result
                    return new StoreItemResult(result.Result, result.Item);
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Unexpected exception while copying data.");
                return new StoreItemResult(DavStatusCode.InternalServerError);
            }
        }

        public override int GetHashCode() => FileInfo.FullName.GetHashCode();

        public override bool Equals(object? obj) =>
                obj is DiskStoreItem storeItem && 
                storeItem.FileInfo.FullName.Equals(FileInfo.FullName, StringComparison.CurrentCultureIgnoreCase);
    }
}
