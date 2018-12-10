﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Locking;
using NWebDav.Server.Logging;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores
{
    [DebuggerDisplay("{_fileInfo.FullPath}")]
    public sealed class DiskStoreItem : IDiskStoreItem
    {
        private static readonly ILogger s_log = LoggerFactory.CreateLogger(typeof(DiskStoreItem));
        private readonly FileInfo _fileInfo;

        public DiskStoreItem(ILockingManager lockingManager, FileInfo fileInfo, bool isWritable)
        {
            LockingManager = lockingManager;
            _fileInfo = fileInfo;
            IsWritable = isWritable;
        }

        public static PropertyManager<DiskStoreItem> DefaultPropertyManager { get; } = new PropertyManager<DiskStoreItem>(new DavProperty<DiskStoreItem>[]
        {
            // RFC-2518 properties
            new DavCreationDate<DiskStoreItem>
            {
                Getter = (context, item) => item._fileInfo.CreationTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.CreationTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new DavDisplayName<DiskStoreItem>
            {
                Getter = (context, item) => item._fileInfo.Name
            },
            new DavGetContentLength<DiskStoreItem>
            {
                Getter = (context, item) => item._fileInfo.Length
            },
            new DavGetContentType<DiskStoreItem>
            {
                Getter = (context, item) => item.DetermineContentType()
            },
            new DavGetEtag<DiskStoreItem>
            {
                // Calculating the Etag is an expensive operation,
                // because we need to scan the entire file.
                IsExpensive = true,
                Getter = (context, item) => item.CalculateEtag()
            },
            new DavGetLastModified<DiskStoreItem>
            {
                Getter = (context, item) => item._fileInfo.LastWriteTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.LastWriteTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new DavGetResourceType<DiskStoreItem>
            {
                Getter = (context, item) => null
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<DiskStoreItem>(),
            new DavSupportedLockDefault<DiskStoreItem>(),

            // Hopmann/Lippert collection properties
            // (although not a collection, the IsHidden property might be valuable)
            new DavExtCollectionIsHidden<DiskStoreItem>
            {
                Getter = (context, item) => (item._fileInfo.Attributes & FileAttributes.Hidden) != 0
            },

            // Win32 extensions
            new Win32CreationTime<DiskStoreItem>
            {
                Getter = (context, item) => item._fileInfo.CreationTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.CreationTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastAccessTime<DiskStoreItem>
            {
                Getter = (context, item) => item._fileInfo.LastAccessTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.LastAccessTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastModifiedTime<DiskStoreItem>
            {
                Getter = (context, item) => item._fileInfo.LastWriteTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.LastWriteTimeUtc = value;
                    return DavStatusCode.Ok;
                }
            },
            new Win32FileAttributes<DiskStoreItem>
            {
                Getter = (context, item) => item._fileInfo.Attributes,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.Attributes = value;
                    return DavStatusCode.Ok;
                }
            }
        });

        public bool IsWritable { get; }
        public string Name => _fileInfo.Name;
        public string UniqueKey => _fileInfo.FullName;
        public string FullPath => _fileInfo.FullName;
        public Task<Stream> GetReadableStreamAsync(IHttpContext httpContext) => Task.FromResult((Stream)_fileInfo.OpenRead());

        public async Task<DavStatusCode> UploadFromStreamAsync(IHttpContext httpContext, Stream inputStream)
        {
            // Check if the item is writable
            if (!IsWritable)
                return DavStatusCode.Conflict;

            // Copy the stream
            try
            {
                // Copy the information to the destination stream
                using (var outputStream = _fileInfo.OpenWrite())
                {
                    await inputStream.CopyToAsync(outputStream, 81920, httpContext.RequestAborted).ConfigureAwait(false);
                }
                return DavStatusCode.Ok;
            }
            catch (IOException ioException) when (ioException.IsDiskFull())
            {
                return DavStatusCode.InsufficientStorage;
            }
        }

        public IPropertyManager PropertyManager => DefaultPropertyManager;
        public ILockingManager LockingManager { get; }

        public async Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, IHttpContext httpContext)
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
                    var result = await destination.CreateItemAsync(name, overwrite, httpContext).ConfigureAwait(false);

                    // Check if the item could be created
                    if (result.Item != null)
                    {
                        using (var sourceStream = await GetReadableStreamAsync(httpContext).ConfigureAwait(false))
                        {
                            var copyResult = await result.Item.UploadFromStreamAsync(httpContext, sourceStream).ConfigureAwait(false);
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
                s_log.Log(LogLevel.Error, () => "Unexpected exception while copying data.", exc);
                return new StoreItemResult(DavStatusCode.InternalServerError);
            }
        }

        public override int GetHashCode()
        {
            return _fileInfo.FullName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DiskStoreItem storeItem))
                return false;
            return storeItem._fileInfo.FullName.Equals(_fileInfo.FullName, StringComparison.CurrentCultureIgnoreCase);
        }

        private string DetermineContentType()
        {
            return MimeTypeHelper.GetMimeType(_fileInfo.Name);
        }

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
