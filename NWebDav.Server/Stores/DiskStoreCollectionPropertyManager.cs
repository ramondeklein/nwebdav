using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores;

public class DiskStoreCollectionPropertyManager : PropertyManager<DiskStoreCollection>
{
    private static readonly XElement s_xDavCollection = new(WebDavNamespaces.DavNs + "collection");
        
    public DiskStoreCollectionPropertyManager(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) : base(GetProperties(httpContextAccessor, lockingManager))
    {
    }

    private static DavProperty<DiskStoreCollection>[] GetProperties(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) => new DavProperty<DiskStoreCollection>[]
    {
        // RFC-2518 properties
        new DavCreationDate<DiskStoreCollection>(httpContextAccessor)
        {
            Getter = collection => collection.DirectoryInfo.CreationTimeUtc,
            Setter = (collection, value) =>
            {
                collection.DirectoryInfo.CreationTimeUtc = value;
                return DavStatusCode.Ok;
            }
        },
        new DavDisplayName<DiskStoreCollection>
        {
            Getter = collection => collection.DirectoryInfo.Name
        },
        new DavGetLastModified<DiskStoreCollection>
        {
            Getter = collection => collection.DirectoryInfo.LastWriteTimeUtc,
            Setter = (collection, value) =>
            {
                collection.DirectoryInfo.LastWriteTimeUtc = value;
                return DavStatusCode.Ok;
            }
        },
        new DavGetResourceType<DiskStoreCollection>
        {
            Getter = _ => new[] { s_xDavCollection }
        },

        // Default locking property handling via the LockingManager
        new DavLockDiscoveryDefault<DiskStoreCollection>(lockingManager),
        new DavSupportedLockDefault<DiskStoreCollection>(lockingManager),

        // Hopmann/Lippert collection properties
        new DavExtCollectionChildCount<DiskStoreCollection>
        {
            Getter = collection => collection.DirectoryInfo.EnumerateFiles().Count() + collection.DirectoryInfo.EnumerateDirectories().Count()
        },
        new DavExtCollectionIsFolder<DiskStoreCollection>
        {
            Getter = _ => true
        },
        new DavExtCollectionIsHidden<DiskStoreCollection>
        {
            Getter = collection => (collection.DirectoryInfo.Attributes & FileAttributes.Hidden) != 0
        },
        new DavExtCollectionIsStructuredDocument<DiskStoreCollection>
        {
            Getter = _ => false
        },
        new DavExtCollectionHasSubs<DiskStoreCollection>
        {
            Getter = collection => collection.DirectoryInfo.EnumerateDirectories().Any()
        },
        new DavExtCollectionNoSubs<DiskStoreCollection>
        {
            Getter = _ => false
        },
        new DavExtCollectionObjectCount<DiskStoreCollection>
        {
            Getter = collection => collection.DirectoryInfo.EnumerateFiles().Count()
        },
        new DavExtCollectionReserved<DiskStoreCollection>
        {
            Getter = collection => !collection.IsWritable
        },
        new DavExtCollectionVisibleCount<DiskStoreCollection>
        {
            Getter = collection =>
                collection.DirectoryInfo.EnumerateDirectories().Count(di => (di.Attributes & FileAttributes.Hidden) == 0) +
                collection.DirectoryInfo.EnumerateFiles().Count(fi => (fi.Attributes & FileAttributes.Hidden) == 0)
        },

        // Win32 extensions
        new Win32CreationTime<DiskStoreCollection>
        {
            Getter = collection => collection.DirectoryInfo.CreationTimeUtc,
            Setter = (collection, value) =>
            {
                collection.DirectoryInfo.CreationTimeUtc = value;
                return DavStatusCode.Ok;
            }
        },
        new Win32LastAccessTime<DiskStoreCollection>
        {
            Getter = collection => collection.DirectoryInfo.LastAccessTimeUtc,
            Setter = (collection, value) =>
            {
                collection.DirectoryInfo.LastAccessTimeUtc = value;
                return DavStatusCode.Ok;
            }
        },
        new Win32LastModifiedTime<DiskStoreCollection>
        {
            Getter = collection => collection.DirectoryInfo.LastWriteTimeUtc,
            Setter = (collection, value) =>
            {
                collection.DirectoryInfo.LastWriteTimeUtc = value;
                return DavStatusCode.Ok;
            }
        },
        new Win32FileAttributes<DiskStoreCollection>
        {
            Getter = collection => collection.DirectoryInfo.Attributes,
            Setter = (collection, value) =>
            {
                collection.DirectoryInfo.Attributes = value;
                return DavStatusCode.Ok;
            }
        }
    };
}