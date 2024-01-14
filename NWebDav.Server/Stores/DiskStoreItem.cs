using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores
{
    [DebuggerDisplay("{_fileInfo.FullPath}")]
    public sealed class DiskStoreItem : IStoreItem
    {
        private readonly DiskStore _diskStore;
        private readonly FileInfo _fileInfo;
        private readonly ILogger<DiskStoreItem> _logger;

        public DiskStoreItem(DiskStore diskStore, FileInfo fileInfo, ILogger<DiskStoreItem> logger)
        {
            _diskStore = diskStore;
            _fileInfo = fileInfo;
            _logger = logger;
        }

        public static PropertyManager<DiskStoreItem> DefaultPropertyManager { get; } = new(new DavProperty<DiskStoreItem>[]
        {
            // RFC-2518 properties
            new DavCreationDate<DiskStoreItem>
            {
                Getter = (_, item) => item._fileInfo.CreationTimeUtc,
                Setter = (_, item, value) =>
                {
                    item._fileInfo.CreationTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new DavDisplayName<DiskStoreItem>
            {
                Getter = (_, item) => item._fileInfo.Name
            },
            new DavGetContentLength<DiskStoreItem>
            {
                Getter = (_, item) => item._fileInfo.Length
            },
            new DavGetContentType<DiskStoreItem>
            {
                Getter = (_, item) => item.DetermineContentType()
            },
            new DavGetEtag<DiskStoreItem>
            {
                // Calculating the Etag is an expensive operation,
                // because we need to scan the entire file.
                IsExpensive = true,
                Getter = (_, item) => item.CalculateEtag()
            },
            new DavGetLastModified<DiskStoreItem>
            {
                Getter = (_, item) => item._fileInfo.LastWriteTimeUtc,
                Setter = (_, item, value) =>
                {
                    item._fileInfo.LastWriteTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new DavGetResourceType<DiskStoreItem>
            {
                Getter = (_, _) => null
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<DiskStoreItem>(),
            new DavSupportedLockDefault<DiskStoreItem>(),

            // Hopmann/Lippert collection properties
            // (although not a collection, the IsHidden property might be valuable)
            new DavExtCollectionIsHidden<DiskStoreItem>
            {
                Getter = (_, item) => (item._fileInfo.Attributes & FileAttributes.Hidden) != 0
            },

            // Win32 extensions
            new Win32CreationTime<DiskStoreItem>
            {
                Getter = (_, item) => item._fileInfo.CreationTimeUtc,
                Setter = (_, item, value) =>
                {
                    item._fileInfo.CreationTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastAccessTime<DiskStoreItem>
            {
                Getter = (_, item) => item._fileInfo.LastAccessTimeUtc,
                Setter = (_, item, value) =>
                {
                    item._fileInfo.LastAccessTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastModifiedTime<DiskStoreItem>
            {
                Getter = (_, item) => item._fileInfo.LastWriteTimeUtc,
                Setter = (_, item, value) =>
                {
                    item._fileInfo.LastWriteTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32FileAttributes<DiskStoreItem>
            {
                Getter = (_, item) => item._fileInfo.Attributes,
                Setter = (_, item, value) =>
                {
                    item._fileInfo.Attributes = value;
                    return DavStatusCode.Ok;
                }
            }
        });

        public bool IsWritable => _diskStore.IsWritable;
        public string Name => _fileInfo.Name;
        public string UniqueKey => _fileInfo.FullName;
        public string FullPath => _fileInfo.FullName;
        public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken) => Task.FromResult((Stream)_fileInfo.OpenRead());

        public async Task<DavStatusCode> UploadFromStreamAsync(Stream inputStream, CancellationToken cancellationToken)
        {
            // Check if the item is writable
            if (!IsWritable)
                return DavStatusCode.Conflict;

            // Copy the stream
            try
            {
                // Copy the information to the destination stream
                await using (var outputStream = _fileInfo.OpenWrite())
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

        public IPropertyManager PropertyManager => DefaultPropertyManager;
        public ILockingManager LockingManager => _diskStore.LockingManager;

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
                    File.Copy(_fileInfo.FullName, destinationPath, true);

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

        public override int GetHashCode() => _fileInfo.FullName.GetHashCode();

        public override bool Equals(object? obj) =>
                obj is DiskStoreItem storeItem && 
                storeItem._fileInfo.FullName.Equals(_fileInfo.FullName, StringComparison.CurrentCultureIgnoreCase);

        private string DetermineContentType() =>  MimeTypeHelper.GetMimeType(_fileInfo.Name);

        private string CalculateEtag()
        {
            using (var stream = File.OpenRead(_fileInfo.FullName))
            {
                var hash = SHA256.Create().ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
