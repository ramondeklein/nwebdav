using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores;

public interface IStoreCollection : IStoreItem
{
    // Get specific item (or all items)
    Task<IStoreItem?> GetItemAsync(string name, CancellationToken cancellationToken);

    IAsyncEnumerable<IStoreItem> GetItemsAsync(CancellationToken cancellationToken);

    // Create items and collections and add to the collection
    Task<StoreItemResult> CreateItemAsync(string name, Stream stream, bool overwrite, CancellationToken cancellationToken);
    Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, CancellationToken cancellationToken);

    // Checks if the collection can be moved directly to the destination
    bool SupportsFastMove(IStoreCollection destination, string destinationName, bool overwrite);

    // Move items between collections
    Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destination, string destinationName, bool overwrite, CancellationToken cancellationToken);

    // Delete items from collection
    Task<DavStatusCode> DeleteItemAsync(string name, CancellationToken cancellationToken);

    InfiniteDepthMode InfiniteDepthMode { get; }
}