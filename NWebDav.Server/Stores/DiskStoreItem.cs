using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;

using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Logging;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores
{
    [DebuggerDisplay("{_fileInfo.FullPath}")]
    public class DiskStoreItem : IStoreItem
    {
        private static readonly ILogger s_log = LoggerFactory.CreateLogger(typeof(DiskStoreItem));
        private readonly FileInfo _fileInfo;

        public DiskStoreItem(ILockingManager lockingManager, FileInfo fileInfo)
        {
            LockingManager = lockingManager;
            _fileInfo = fileInfo;
        }

        private static PropertyManager<DiskStoreItem> DefaultPropertyManager { get; } = new PropertyManager<DiskStoreItem>(new DavProperty<DiskStoreItem>[]
        {
                // RFC-2518 properties
                new DavCreationDate<DiskStoreItem>
                {
                    Getter = (principal, item) => item._fileInfo.CreationTimeUtc,
                    Setter = (principal, item, value) => { item._fileInfo.CreationTimeUtc = value; return DavStatusCode.Ok; }
                },
                new DavDisplayName<DiskStoreItem>
                {
                    Getter = (principal, item) => item._fileInfo.Name
                },
                new DavGetContentLength<DiskStoreItem>
                {
                    Getter = (principal, item) => item._fileInfo.Length
                },
                new DavGetContentType<DiskStoreItem>
                {
                    Getter = (principal, item) => item.DetermineContentType()
                },
                new DavGetEtag<DiskStoreItem>
                {
                    // Calculating the Etag is an expensive operation,
                    // because we need to scan the entire file.
                    IsExpensive = true,
                    Getter = (principal, item) => item.CalculateEtag()
                },
                new DavGetLastModified<DiskStoreItem>
                {
                    Getter = (principal, item) => item._fileInfo.LastWriteTimeUtc,
                    Setter = (principal, item, value) => { item._fileInfo.LastWriteTimeUtc = value; return DavStatusCode.Ok; }
                },
                new DavGetResourceType<DiskStoreItem>
                {
                    Getter = (principal, item) => null
                },

                // Default locking property handling via the LockingManager
                new DavLockDiscoveryDefault<DiskStoreItem>(),
                new DavSupportedLockDefault<DiskStoreItem>(),

                // Hopmann/Lippert collection properties
                // (although not a collection, the IsHidden property might be valuable)
                new DavIsHidden<DiskStoreItem>
                {
                    Getter = (principal, item) => (item._fileInfo.Attributes & FileAttributes.Hidden) != 0
                },

                // Win32 extensions
                new Win32CreationTime<DiskStoreItem>
                {
                    Getter = (principal, item) => item._fileInfo.CreationTimeUtc,
                    Setter = (principal, item, value) => { item._fileInfo.CreationTimeUtc = value; return DavStatusCode.Ok; }
                },
                new Win32LastAccessTime<DiskStoreItem>
                {
                    Getter = (principal, item) => item._fileInfo.LastAccessTimeUtc,
                    Setter = (principal, item, value) => { item._fileInfo.LastAccessTimeUtc = value; return DavStatusCode.Ok; }
                },
                new Win32LastModifiedTime<DiskStoreItem>
                {
                    Getter = (principal, item) => item._fileInfo.LastWriteTimeUtc,
                    Setter = (principal, item, value) => { item._fileInfo.LastWriteTimeUtc = value; return DavStatusCode.Ok; }
                },
                new Win32FileAttributes<DiskStoreItem>
                {
                    Getter = (principal, item) => item._fileInfo.Attributes,
                    Setter = (principal, item, value) => { item._fileInfo.Attributes = value; return DavStatusCode.Ok; }
                }
        });

        public string Name => _fileInfo.Name;
        public Stream GetReadableStream(IPrincipal principal) => _fileInfo.OpenRead();
        public Stream GetWritableStream(IPrincipal principal) => _fileInfo.OpenWrite();
        public IPropertyManager PropertyManager => DefaultPropertyManager;
        public ILockingManager LockingManager { get; }

        public async Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, IPrincipal principal)
        {
            try
            {
                // If the destination is also a disk-store, then we can use the FileCopy API
                // (it's probably a bit more efficient than copying in C#)
                var diskCollection = destination as DiskStoreCollection;
                if (diskCollection != null)
                {
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
                    var result = await destination.CreateItemAsync(name, overwrite, principal).ConfigureAwait(false);

                    // Check if the item could be created
                    if (result.Item != null)
                    {
                        using (var destinationStream = result.Item.GetWritableStream(principal))
                        using (var sourceStream = GetReadableStream(principal))
                        {
                            await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);
                        }
                    }

                    // Return result
                    return new StoreItemResult(result.Result, result.Item);
                }
            }
            catch (IOException ioException) when (ioException.IsDiskFull())
            {
                s_log.Log(LogLevel.Error, "Out of disk space while copying data.", ioException);
                return new StoreItemResult(DavStatusCode.InsufficientStorage);
            }
            catch (Exception exc)
            {
                s_log.Log(LogLevel.Error, "Unexpected exception while copying data.", exc);
                return new StoreItemResult(DavStatusCode.InternalServerError);
            }
        }

        public override int GetHashCode()
        {
            return _fileInfo.FullName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var storeItem = obj as DiskStoreItem;
            if (storeItem == null)
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
