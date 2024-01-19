using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using NWebDav.Server;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal class AzureContainerCollectionPropertyManager : PropertyManager<AzureContainerCollection>
{
    private static readonly XElement s_xDavCollection = new(WebDavNamespaces.DavNs + "collection");
        
    public AzureContainerCollectionPropertyManager(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) : base(GetProperties(httpContextAccessor, lockingManager))
    {
    }

    private static DavProperty<AzureContainerCollection>[] GetProperties(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) => new DavProperty<AzureContainerCollection>[]
    {
        // RFC-2518 properties
        new DavCreationDate<AzureContainerCollection>(httpContextAccessor)
        {
            Getter = _ => DateTime.UnixEpoch,
        },
        new DavDisplayName<AzureContainerCollection>
        {
            Getter = collection => collection.Name
        },
        new DavGetLastModified<AzureContainerCollection>
        {
            Getter = _ => DateTime.UnixEpoch,
        },
        new DavGetResourceType<AzureContainerCollection>
        {
            Getter = _ => new[] { s_xDavCollection }
        },

        // Default locking property handling via the LockingManager
        new DavLockDiscoveryDefault<AzureContainerCollection>(lockingManager),
        new DavSupportedLockDefault<AzureContainerCollection>(lockingManager),

        // Hopmann/Lippert collection properties
        new DavExtCollectionChildCount<AzureContainerCollection>
        {
            GetterAsync = async (collection, ct) => await collection.GetChildItems(ct).CountAsync(ct).ConfigureAwait(false)
        },
        new DavExtCollectionIsFolder<AzureContainerCollection>
        {
            Getter = _ => true
        },
        new DavExtCollectionIsHidden<AzureContainerCollection>
        {
            Getter = _ => false,
            Setter = (_, _) => DavStatusCode.Conflict
        },
        new DavExtCollectionIsStructuredDocument<AzureContainerCollection>
        {
            Getter = _ => false
        },
        new DavExtCollectionHasSubs<AzureContainerCollection>
        {
            GetterAsync = async (collection, ct) => await collection.GetChildItems(ct).AnyAsync(ct).ConfigureAwait(false)
        },
        new DavExtCollectionNoSubs<AzureContainerCollection>
        {
            Getter = _ => false
        },
        new DavExtCollectionObjectCount<AzureContainerCollection>
        {
            Getter = _ => 0
        },
        new DavExtCollectionReserved<AzureContainerCollection>
        {
            Getter = _ => false
        },
        new DavExtCollectionVisibleCount<AzureContainerCollection>
        {
            GetterAsync = async (collection, ct) => await collection.GetChildItems(ct).CountAsync(ct).ConfigureAwait(false)
        },

        // Win32 extensions
        new Win32CreationTime<AzureContainerCollection>
        {
            Getter = _ => DateTime.UnixEpoch
        },
        new Win32LastAccessTime<AzureContainerCollection>
        {
            Getter = _ => DateTime.UnixEpoch
        },
        new Win32LastModifiedTime<AzureContainerCollection>
        {
            Getter = _ => DateTime.UnixEpoch
        },
        new Win32FileAttributes<AzureContainerCollection>
        {
            Getter = _ => FileAttributes.Directory
        }
    };
}