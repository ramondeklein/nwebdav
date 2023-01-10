using NWebDav.Server.Enums;
using NWebDav.Server.Http;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores
{
    public struct StoreItemResult
    {
        public HttpStatusCode Result { get; }
        public IStoreItem Item { get; }

        public StoreItemResult(HttpStatusCode result, IStoreItem item = null)
        {
            Result = result;
            Item = item;
        }

        public static bool operator!=(StoreItemResult left, StoreItemResult right)
        {
            return !(left == right);
        }

        public static bool operator==(StoreItemResult left, StoreItemResult right)
        {
            return left.Result == right.Result && (left.Item == null && right.Item == null || left.Item != null && left.Item.Equals(right.Item));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StoreItemResult))
                return false;
            return this == (StoreItemResult)obj;
        }

        public override int GetHashCode() => Result.GetHashCode() ^ (Item?.GetHashCode() ?? 0);
    }

    public struct StoreCollectionResult
    {
        public HttpStatusCode Result { get; }
        public IStoreCollection Collection { get; }

        public StoreCollectionResult(HttpStatusCode result, IStoreCollection collection = null)
        {
            Result = result;
            Collection = collection;
        }

        public static bool operator !=(StoreCollectionResult left, StoreCollectionResult right)
        {
            return !(left == right);
        }

        public static bool operator ==(StoreCollectionResult left, StoreCollectionResult right)
        {
            return left.Result == right.Result && (left.Collection == null && right.Collection == null || left.Collection != null && left.Collection.Equals(right.Collection));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StoreCollectionResult))
                return false;
            return this == (StoreCollectionResult)obj;
        }

        public override int GetHashCode() => Result.GetHashCode() ^ (Collection?.GetHashCode() ?? 0);
    }

    public interface IStore // TODO(wd): Replace with IStorageService
    {
        Task<IStoreItem> GetItemAsync(Uri uri, IHttpContext context);
        Task<IStoreCollection> GetCollectionAsync(Uri uri, IHttpContext context);
    }

    public interface IStoreItem // TODO(wd): Replace with IDavFile, IDavStorable
    {
        // Item properties
        string Name { get; }
        string UniqueKey { get; }

        // Read/Write access to the data
        Task<Stream> GetReadableStreamAsync(IHttpContext context);
        Task<HttpStatusCode> UploadFromStreamAsync(IHttpContext context, Stream source);

        // Copy support
        Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, IHttpContext context);

        // Property support
        IPropertyManager PropertyManager { get; }

        // Locking support
        ILockingManager LockingManager { get; }
    }

    public interface IStoreCollection : IStoreItem // TODO(wd): Replace with IDavFolder, IDavStorable
    {

        

        // Get specific item (or all items)
        Task<IStoreItem> GetItemAsync(string name, IHttpContext context);

        Task<IEnumerable<IStoreItem>> GetItemsAsync(IHttpContext context);

        // Create items and collections and add to the collection
        Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, IHttpContext context);
        Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, IHttpContext context);

        // Checks if the collection can be moved directly to the destination
        bool SupportsFastMove(IStoreCollection destination, string destinationName, bool overwrite, IHttpContext context);

        // Move items between collections
        Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destination, string destinationName, bool overwrite, IHttpContext context);

        // Delete items from collection
        Task<HttpStatusCode> DeleteItemAsync(string name, IHttpContext context);

        EnumerationDepthMode InfiniteDepthMode { get; }
    }
}
