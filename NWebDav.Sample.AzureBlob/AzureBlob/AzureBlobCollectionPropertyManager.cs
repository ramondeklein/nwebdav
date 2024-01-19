using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using NWebDav.Server;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal class AzureBlobCollectionPropertyManager : PropertyManager<AzureBlobCollection>
{
    private static readonly XElement s_xDavCollection = new(WebDavNamespaces.DavNs + "collection");
        
    public AzureBlobCollectionPropertyManager(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) : base(GetProperties(httpContextAccessor, lockingManager))
    {
    }

    private static DavProperty<AzureBlobCollection>[] GetProperties(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) => new DavProperty<AzureBlobCollection>[]
    {
        // RFC-2518 properties
        new DavCreationDate<AzureBlobCollection>(httpContextAccessor)
        {
            Getter = collection => collection.StoreItemMetadata.CreatedOn,
        },
        new DavDisplayName<AzureBlobCollection>
        {
            Getter = collection => collection.Name
        },
        new DavGetLastModified<AzureBlobCollection>
        {
            Getter = collection => collection.StoreItemMetadata.CreatedOn,
        },
        new DavGetResourceType<AzureBlobCollection>
        {
            Getter = _ => new[] { s_xDavCollection }
        },

        // Default locking property handling via the LockingManager
        new DavLockDiscoveryDefault<AzureBlobCollection>(lockingManager),
        new DavSupportedLockDefault<AzureBlobCollection>(lockingManager),

        // Hopmann/Lippert collection properties
        new DavExtCollectionChildCount<AzureBlobCollection>
        {
            GetterAsync = async (collection, ct) => await collection.GetChildItems(ct).CountAsync(ct).ConfigureAwait(false)
        },
        new DavExtCollectionIsFolder<AzureBlobCollection>
        {
            Getter = _ => true
        },
        new DavExtCollectionIsHidden<AzureBlobCollection>
        {
            Getter = _ => false,
            Setter = (_, _) => DavStatusCode.Conflict
        },
        new DavExtCollectionIsStructuredDocument<AzureBlobCollection>
        {
            Getter = _ => false
        },
        new DavExtCollectionHasSubs<AzureBlobCollection>
        {
            GetterAsync = async (collection, ct) => await collection.GetChildItems(ct).AnyAsync(ct).ConfigureAwait(false)
        },
        new DavExtCollectionNoSubs<AzureBlobCollection>
        {
            Getter = _ => false
        },
        new DavExtCollectionObjectCount<AzureBlobCollection>
        {
            GetterAsync = async (collection, ct) => await collection.GetChildItems(ct).Where(b => !b.IsFolder).CountAsync(ct).ConfigureAwait(false)
        },
        new DavExtCollectionReserved<AzureBlobCollection>
        {
            Getter = collection => !collection.IsWritable
        },
        new DavExtCollectionVisibleCount<AzureBlobCollection>
        {
            GetterAsync = async (collection, ct) => await collection.GetChildItems(ct).CountAsync(ct).ConfigureAwait(false)
        },

        // Win32 extensions
        new Win32CreationTime<AzureBlobCollection>
        {
            Getter = collection => collection.StoreItemMetadata.CreatedOn
        },
        new Win32LastAccessTime<AzureBlobCollection>
        {
            Getter = collection => collection.StoreItemMetadata.CreatedOn
        },
        new Win32LastModifiedTime<AzureBlobCollection>
        {
            Getter = collection => collection.StoreItemMetadata.CreatedOn
        },
        new Win32FileAttributes<AzureBlobCollection>
        {
            Getter = _ => FileAttributes.Directory
        }
    };
}