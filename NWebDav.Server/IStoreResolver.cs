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
        Task<StoreItemResult> CopyToAsync(IStoreCollection destination, string name, bool overwrite, IPrincipal principal);

        // Property support
        IPropertyManager PropertyManager { get; }
    }

    public interface IStoreCollection : IStoreItem
    {
        Task<IList<IStoreItem>> GetItemsAsync(IPrincipal principal);
        Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, IPrincipal principal);
        Task<StoreItemResult> CreateCollectionAsync(string name, bool overwrite, IPrincipal principal);
        Task<DavStatusCode> DeleteItemAsync(string name, IPrincipal principal);
        Task<DavStatusCode> DeleteCollectionAsync(IPrincipal principal);

        bool AllowInfiniteDepthProperties { get; }
    }
}
