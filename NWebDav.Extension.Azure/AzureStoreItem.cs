using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NWebDav.Server;
using NWebDav.Server.Http;
using NWebDav.Server.Locking;
using NWebDav.Server.Logging;
using NWebDav.Server.Props;
using NWebDav.Server.Stores;

namespace NWebDav.Extension.Azure
{
    [DebuggerDisplay("{_azureBlob.FullName}")]
    public sealed class AzureStoreItem : IStoreItem
    {
        private static readonly ILogger s_log = LoggerFactory.CreateLogger(typeof(DiskStoreItem));
        private readonly AzureBlob _azureBlob;

        public AzureStoreItem(ILockingManager lockingManager, AzureBlob azureBlob, bool isWritable)
        {
            LockingManager = lockingManager;
            IsWritable = isWritable;

            _azureBlob = azureBlob;
        }

        public static PropertyManager<AzureStoreItem> DefaultPropertyManager { get; } = new PropertyManager<AzureStoreItem>(new DavProperty<AzureStoreItem>[]
        {
            // RFC-2518 properties
            new DavCreationDate<AzureStoreItem>
            {
                Getter = (context, collection) => collection._azureBlob.CreationTimeUtc,
                SetterAsync = async (context, collection, value) =>
                {
                    await collection._azureBlob.SetCreationTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new DavDisplayName<AzureStoreItem>
            {
                Getter = (context, collection) => collection._azureBlob.Name
            },
            new DavGetContentLength<AzureStoreItem>
            {
                Getter = (context, item) => item._azureBlob.ContentLength
            },
            new DavGetContentType<AzureStoreItem>
            {
                Getter = (context, item) => item._azureBlob.ContentType
            },
            new DavGetEtag<AzureStoreItem>
            {
                Getter = (context, item) => item._azureBlob.ETag
            },
            new DavGetLastModified<AzureStoreItem>
            {
                Getter = (context, item) => item._azureBlob.LastWriteTimeUtc,
                SetterAsync = async (context, item, value) =>
                {
                    await item._azureBlob.SetLastWriteTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new DavGetResourceType<AzureStoreItem>
            {
                Getter = (context, item) => null
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<AzureStoreItem>(),
            new DavSupportedLockDefault<AzureStoreItem>(),

            // Hopmann/Lippert collection properties
            // (although not a collection, the IsHidden property might be valuable)
            new DavExtCollectionIsHidden<AzureStoreItem>
            {
                Getter = (context, item) => (item._azureBlob.Attributes & FileAttributes.Hidden) != 0
            },

            // Win32 extensions
            new Win32CreationTime<AzureStoreItem>
            {
                Getter = (context, item) => item._azureBlob.CreationTimeUtc,
                SetterAsync = async (context, item, value) =>
                {
                    await item._azureBlob.SetCreationTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastAccessTime<AzureStoreItem>
            {
                Getter = (context, item) => item._azureBlob.LastAccessTimeUtc,
                SetterAsync = async (context, item, value) =>
                {
                    await item._azureBlob.SetLastAccessTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new Win32LastModifiedTime<AzureStoreItem>
            {
                Getter = (context, item) => item._azureBlob.LastWriteTimeUtc,
                SetterAsync = async (context, item, value) =>
                {
                    await item._azureBlob.SetLastWriteTimeUtcAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            },
            new Win32FileAttributes<AzureStoreItem>
            {
                Getter = (context, item) => item._azureBlob.Attributes,
                SetterAsync = async (context, item, value) =>
                {
                    await item._azureBlob.SetAttributesAsync(value).ConfigureAwait(false);
                    return DavStatusCode.Ok;
                }
            }
        });

        public bool IsWritable { get; }
        public string Name => _azureBlob.Name;
        public string UniqueKey => _azureBlob.FullName;
        public IPropertyManager PropertyManager => DefaultPropertyManager;
        public ILockingManager LockingManager { get; }

        public Task<Stream> GetReadableStreamAsync(IHttpContext httpContext)
        {
            return _azureBlob.GetReadStreamAsync();
        }

        public async Task<DavStatusCode> UploadFromStreamAsync(IHttpContext httpContext, Stream inputStream)
        {
            // Check if the item is writable
            if (!IsWritable)
                return DavStatusCode.Conflict;

            // Upload from the input stream
            await _azureBlob.UploadFromStreamAsync(inputStream).ConfigureAwait(false);
            return DavStatusCode.Ok;
        }

        public async Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, IHttpContext httpContext)
        {
            try
            {
                // If the destination is also a azure-store, then we can use the Azure API
                // (it's probably a bit more efficient than copying in C#)
                var azureStoreCollection = destination as AzureStoreCollection;
                if (azureStoreCollection != null)
                {
                    // Check if the collection is writable
                    if (!azureStoreCollection.IsWritable)
                        return new StoreItemResult(DavStatusCode.PreconditionFailed);

                    var destinationResult = await azureStoreCollection.CreateItemAsync(name, overwrite, httpContext).ConfigureAwait(false);
                    if (destinationResult.Result == DavStatusCode.Created || destinationResult.Result == DavStatusCode.NoContent)
                    {
                        var destinationItem = (AzureStoreItem) destinationResult.Item;
                        var copyResult = await _azureBlob.CopyAsync(destinationItem._azureBlob).ConfigureAwait(false);
                        if (copyResult != DavStatusCode.Ok)
                            return new StoreItemResult(copyResult);
                    }

                    // Return the appropriate status
                    return new StoreItemResult(destinationResult.Result);
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
                s_log.Log(LogLevel.Error, "Unexpected exception while copying data.", exc);
                return new StoreItemResult(DavStatusCode.InternalServerError);
            }
        }

        public override int GetHashCode()
        {
            return _azureBlob.FullName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var storeItem = obj as AzureStoreItem;
            if (storeItem == null)
                return false;
            return storeItem._azureBlob.FullName.Equals(_azureBlob.FullName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}