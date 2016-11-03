using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server;
using NWebDav.Server.Http;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;
using NWebDav.Server.Stores;

namespace NWebDav.Extension.Azure
{
    [DebuggerDisplay("{_azureBlob.FullName}/")]
    public sealed class AzureStoreCollection : IStoreCollection
    {
        private readonly AzureBlob _azureBlob;

        public AzureStoreCollection(ILockingManager lockingManager, AzureBlob azureBlob, bool isWritable)
        {
            LockingManager = lockingManager;
            IsWritable = isWritable;

            _azureBlob = azureBlob;
        }

        public static PropertyManager<AzureStoreCollection> DefaultPropertyManager { get; } = new PropertyManager<AzureStoreCollection>(new DavProperty<AzureStoreCollection>[]
        {
            // RFC-2518 properties
            new DavCreationDate<AzureStoreCollection>
            {
                Getter = (context, collection) => collection._azureBlob.CreationTimeUtc,
                SetterAsync = async (context, collection, value) =>
                {
                    await collection._azureBlob.SetCreationTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new DavDisplayName<AzureStoreCollection>
            {
                Getter = (context, collection) => collection._azureBlob.Name
            },
            new DavGetLastModified<AzureStoreCollection>
            {
                Getter = (context, collection) => collection._azureBlob.LastWriteTimeUtc,
                SetterAsync = async (context, collection, value) =>
                {
                    await collection._azureBlob.SetLastWriteTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new DavGetResourceType<AzureStoreCollection>
            {
                Getter = (context, collection) => new XElement(WebDavNamespaces.DavNs + "collection")
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<AzureStoreCollection>(),
            new DavSupportedLockDefault<AzureStoreCollection>(),

            // Hopmann/Lippert collection properties
            new DavExtCollectionChildCount<AzureStoreCollection>
            {
                GetterAsync = async (context, collection) =>
                {
                    var items = await collection._azureBlob.GetItemsAsync().ConfigureAwait(false);
                    var collections = await collection._azureBlob.GetCollectionsAsync().ConfigureAwait(false);
                    return items.Count() + collections.Count();
                }
            },
            new DavExtCollectionIsFolder<AzureStoreCollection>
            {
                Getter = (context, collection) => true
            },
            new DavExtCollectionIsHidden<AzureStoreCollection>
            {
                Getter = (context, collection) => (collection._azureBlob.Attributes & FileAttributes.Hidden) != 0
            },
            new DavExtCollectionIsStructuredDocument<AzureStoreCollection>
            {
                Getter = (context, collection) => false
            },
            new DavExtCollectionHasSubs<AzureStoreCollection>
            {
                GetterAsync = async (context, collection) =>
                {
                    var collections = await collection._azureBlob.GetCollectionsAsync().ConfigureAwait(false);
                    return collections.Any();
                }
            },
            new DavExtCollectionNoSubs<AzureStoreCollection>
            {
                Getter = (context, collection) => false
            },
            new DavExtCollectionObjectCount<AzureStoreCollection>
            {
                GetterAsync = async (context, collection) =>
                {
                    var collections = await collection._azureBlob.GetCollectionsAsync().ConfigureAwait(false);
                    return collections.Count();
                }
            },
            new DavExtCollectionReserved<AzureStoreCollection>
            {
                Getter = (context, collection) => !collection.IsWritable
            },
            new DavExtCollectionVisibleCount<AzureStoreCollection>
            {
                GetterAsync = async (context, collection) =>
                {
                    var items = await collection._azureBlob.GetItemsAsync().ConfigureAwait(false);
                    var collections = await collection._azureBlob.GetCollectionsAsync().ConfigureAwait(false);
                    return items.Count(i => (i.Attributes & FileAttributes.Hidden) == 0) + collections.Count(i => (i.Attributes & FileAttributes.Hidden) == 0);
                }
            },

            // Win32 extensions
            new Win32CreationTime<AzureStoreCollection>
            {
                Getter = (context, collection) => collection._azureBlob.CreationTimeUtc,
                SetterAsync = async (context, collection, value) =>
                {
                    await collection._azureBlob.SetCreationTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastAccessTime<AzureStoreCollection>
            {
                Getter = (context, collection) => collection._azureBlob.LastAccessTimeUtc,
                SetterAsync = async (context, collection, value) =>
                {
                    await collection._azureBlob.SetLastAccessTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastModifiedTime<AzureStoreCollection>
            {
                Getter = (context, collection) => collection._azureBlob.LastWriteTimeUtc,
                SetterAsync = async (context, collection, value) =>
                {
                    await collection._azureBlob.SetLastWriteTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new Win32FileAttributes<AzureStoreCollection>
            {
                Getter = (context, collection) => collection._azureBlob.Attributes,
                SetterAsync = async (context, collection, value) =>
                {
                    await collection._azureBlob.SetAttributesAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            }
        });

        public bool IsWritable { get; }
        public string Name => _azureBlob.Name;
        public string UniqueKey => _azureBlob.FullName;

        public Task<Stream> GetReadableStreamAsync(IHttpContext httpContext) => Task.FromResult((Stream)null);
        public Task<DavStatusCode> UploadFromStreamAsync(IHttpContext httpContext, Stream source) => Task.FromResult(DavStatusCode.Conflict);

        public IPropertyManager PropertyManager => DefaultPropertyManager;
        public ILockingManager LockingManager { get; }

        public Task<IStoreItem> GetItemAsync(string name, IHttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        public async Task<IList<IStoreItem>> GetItemsAsync(IHttpContext httpContext)
        {
            await _azureBlob.GetChildsAsync().ConfigureAwait(false);
            throw new NotImplementedException();
        }

        public Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, IHttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        public Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, IHttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        public Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, IHttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        public Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destination, string destinationName, bool overwrite, IHttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        public Task<DavStatusCode> DeleteItemAsync(string name, IHttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        public bool AllowInfiniteDepthProperties => false;

        public override int GetHashCode()
        {
            return _azureBlob.FullName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var storeCollection = obj as AzureStoreCollection;
            if (storeCollection == null)
                return false;
            return storeCollection._azureBlob.FullName.Equals(_azureBlob.FullName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}