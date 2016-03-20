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

namespace NWebDav.Server
{
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

    public interface IStoreCollectionEntry
    {
        // Item properties
        string Name { get; }
        string ContentLanguage { get; }
        long? ContentLength { get; }
        string ContentType { get; }
        DateTime? CreationUtc { get; }
        DateTime? LastModifiedUtc { get; }
        string Etag { get; }

        // Property support
        IList<XName> CheapProperties { get; }
        IList<XName> ExpensiveProperties { get; }
        string GetProperty(XName propertyName);
        bool SetProperty(XName propertyName, string value);
    }

    public interface IStoreCollection : IStoreCollectionEntry
    {
        Task<IList<IStoreCollectionEntry>> GetEntriesAsync(IPrincipal principal);
        Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, IPrincipal principal);
        Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, IPrincipal principal);
        Task<StoreCollectionResult> CopyToAsync(IStoreCollection destinationCollection, string name, bool overwrite, IPrincipal principal);
        Task<DavStatusCode> DeleteItemAsync(string name, IPrincipal principal);
        Task<DavStatusCode> DeleteAsync(IPrincipal principal);

        bool AllowInfiniteDepthProperties { get; }
    }

    public interface IStoreItem : IStoreCollectionEntry
    {
        Stream GetReadableStream(IPrincipal principal);
        Stream GetWritableStream(IPrincipal principal);
        Task<DavStatusCode> CopyToAsync(IStoreCollection destination, string name, bool overwrite, IPrincipal principal);
    }
}
