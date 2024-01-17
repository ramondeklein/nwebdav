using System;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores;

public readonly record struct StoreItemResult(DavStatusCode Result, IStoreItem? Item = null);
public readonly record struct StoreCollectionResult(DavStatusCode Result, IStoreCollection? Collection = null);

public interface IStore
{
    Task<IStoreItem?> GetItemAsync(Uri uri, CancellationToken cancellationToken);
    Task<IStoreCollection?> GetCollectionAsync(Uri uri, CancellationToken cancellationToken);
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