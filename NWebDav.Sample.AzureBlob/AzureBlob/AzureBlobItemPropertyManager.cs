using System.IO;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;

namespace NWebDav.Sample.AzureBlob.AzureBlob;

internal class AzureBlobItemPropertyManager : PropertyManager<AzureBlobItem>
{
    public AzureBlobItemPropertyManager(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) : base(GetProperties(httpContextAccessor, lockingManager))
    {
    }

    private static DavProperty<AzureBlobItem>[] GetProperties(IHttpContextAccessor httpContextAccessor, ILockingManager lockingManager) => new DavProperty<AzureBlobItem>[]
    {
        // RFC-2518 properties
        new DavCreationDate<AzureBlobItem>(httpContextAccessor)
        {
            Getter = item => item.StoreItemMetadata.CreatedOn,
        },
        new DavDisplayName<AzureBlobItem>
        {
            Getter = item => item.Name
        },
        new DavGetContentLength<AzureBlobItem>
        {
            Getter = item => item.StoreItemMetadata.ContentLength
        },
        new DavGetContentType<AzureBlobItem>
        {
            Getter = item => item.StoreItemMetadata.ContentType
        },
        new DavGetEtag<AzureBlobItem>
        {
            Getter = item => item.StoreItemMetadata.ETag.ToString()
        },
        new DavGetLastModified<AzureBlobItem>
        {
            Getter = item => item.StoreItemMetadata.CreatedOn,
        },
        new DavGetResourceType<AzureBlobItem>
        {
            Getter = _ => null
        },

        // Default locking property handling via the LockingManager
        new DavLockDiscoveryDefault<AzureBlobItem>(lockingManager),
        new DavSupportedLockDefault<AzureBlobItem>(lockingManager),

        // Hopmann/Lippert collection properties
        // (although not a collection, the IsHidden property might be valuable)
        new DavExtCollectionIsHidden<AzureBlobItem>
        {
            Getter = _ => false
        },

        // Win32 extensions
        new Win32CreationTime<AzureBlobItem>
        {
            Getter = item => item.StoreItemMetadata.CreatedOn,
        },
        new Win32LastAccessTime<AzureBlobItem>
        {
            Getter = item => item.StoreItemMetadata.CreatedOn,
        },
        new Win32LastModifiedTime<AzureBlobItem>
        {
            Getter = item => item.StoreItemMetadata.CreatedOn,
        },
        new Win32FileAttributes<AzureBlobItem>
        {
            Getter = _ => FileAttributes.Normal
        }
    };
}