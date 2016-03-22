using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Props;

namespace NWebDav.Server
{
    public struct StoreItemResult
    {
        public DavStatusCode Result { get; }
        public IStoreItem Item { get; }

        public StoreItemResult(DavStatusCode result, IStoreItem item = null)
        {
            Result = result;
            Item = item;
        }
    }

    public struct StoreCollectionResult
    {
        public DavStatusCode Result { get; }
        public IStoreCollection Collection { get; }

        public StoreCollectionResult(DavStatusCode result, IStoreCollection collection = null)
        {
            Result = result;
            Collection = collection;
        }
    }

    public interface IStoreResolver
    {
        Task<IStoreItem> GetItemAsync(Uri uri, IPrincipal principal);
        Task<IStoreCollection> GetCollectionAsync(Uri uri, IPrincipal principal);
    }

    public interface IStoreItem
    {
        // Item properties
        string Name { get; }

        // Read/Write access to the data
        Stream GetReadableStream(IPrincipal principal);
        Stream GetWritableStream(IPrincipal principal);

        // Copy support
        Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, IPrincipal principal);

        // Property support
        IPropertyManager PropertyManager { get; }
    }

    public interface IStoreCollection : IStoreItem
    {
        // Get specific item (or all items)
        Task<IStoreItem> GetItemAsync(string name, IPrincipal principal);
        Task<IList<IStoreItem>> GetItemsAsync(IPrincipal principal);

        // Create items and collections and add to the collection
        Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, IPrincipal principal);
        Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, IPrincipal principal);

        // Move items between collections
        Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destination, string destinationName, bool overwrite, IPrincipal principal);

        // Delete items from collection
        Task<DavStatusCode> DeleteItemAsync(string name, IPrincipal principal);

        bool AllowInfiniteDepthProperties { get; }
    }
}
