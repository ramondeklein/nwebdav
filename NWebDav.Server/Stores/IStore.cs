using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores
{
    public readonly record struct StoreItemResult(DavStatusCode Result, IStoreItem? Item = null)
    {
        public override int GetHashCode() => Result.GetHashCode() ^ (Item?.GetHashCode() ?? 0);
    }

    public readonly record struct StoreCollectionResult(DavStatusCode Result, IStoreCollection? Collection = null)
    {
        public override int GetHashCode() => Result.GetHashCode() ^ (Collection?.GetHashCode() ?? 0);
    }

    public interface IStore
    {
        Task<IStoreItem?> GetItemAsync(Uri uri, CancellationToken cancellationToken);
        Task<IStoreCollection?> GetCollectionAsync(Uri uri, CancellationToken cancellationToken);
    }

    public interface IStoreItem
    {
        // Item properties
        string Name { get; }
        string UniqueKey { get; }

        // Read/Write access to the data
        Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken);
        Task<DavStatusCode> UploadFromStreamAsync(Stream source, CancellationToken cancellationToken);

        // Copy support
        Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken);

        // Property support
        IPropertyManager? PropertyManager { get; }

        // Locking support
        ILockingManager? LockingManager { get; }
    }

    public interface IStoreCollection : IStoreItem
    {
        // Get specific item (or all items)
        Task<IStoreItem?> GetItemAsync(string name, CancellationToken cancellationToken);

        Task<IEnumerable<IStoreItem>> GetItemsAsync(CancellationToken cancellationToken);

        // Create items and collections and add to the collection
        Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, CancellationToken cancellationToken);
        Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, CancellationToken cancellationToken);

        // Checks if the collection can be moved directly to the destination
        bool SupportsFastMove(IStoreCollection destination, string destinationName, bool overwrite);

        // Move items between collections
        Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destination, string destinationName, bool overwrite, CancellationToken cancellationToken);

        // Delete items from collection
        Task<DavStatusCode> DeleteItemAsync(string name, CancellationToken cancellationToken);

        InfiniteDepthMode InfiniteDepthMode { get; }
    }

    /// <summary>
    /// When the Depth is set to infinite, then this enumeration specifies
    /// how to deal with this.
    /// </summary>
    public enum InfiniteDepthMode
    {
        /// <summary>
        /// Infinite depth is allowed (this is according spec).
        /// </summary>
        Allowed,

        /// <summary>
        /// Infinite depth is not allowed (this results in HTTP 403 Forbidden).
        /// </summary>
        Rejected,

        /// <summary>
        /// Infinite depth is handled as Depth 0.
        /// </summary>
        Assume0,

        /// <summary>
        /// Infinite depth is handled as Depth 1.
        /// </summary>
        Assume1
    }
}
