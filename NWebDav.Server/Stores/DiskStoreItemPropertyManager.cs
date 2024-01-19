using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores;

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
                var stream = File.OpenRead(item.FileInfo.FullName);
                await using (stream.ConfigureAwait(false))
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