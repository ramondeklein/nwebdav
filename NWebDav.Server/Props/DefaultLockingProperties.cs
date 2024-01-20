using System.Linq;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props;

/// <summary>
/// Default implementation to describe the active locks on a resource.
/// </summary>
/// <remarks>
/// This property implementation calls the
/// <see cref="ILockingManager.GetActiveLockInfo"/>
/// of the item's <see cref="IStoreItem.LockingManager"/> to determine the
/// active locks.
/// </remarks>
/// <typeparam name="TEntry">
/// Store item or collection to which this DAV property applies.
/// </typeparam>
public sealed class DavLockDiscoveryDefault<TEntry> : DavLockDiscovery<TEntry> where TEntry : IStoreItem
{
    /// <summary>
    /// Create an instance of the <see cref="DavLockDiscovery{TEntry}"/>
    /// property that implements the property using the
    /// <see cref="ILockingManager.GetActiveLockInfoAsync"/> 
    /// method of the item's locking manager.
    /// </summary>
    public DavLockDiscoveryDefault(ILockingManager lockingManager)
    {
        GetterAsync = async (item, ct) =>
        {
            var locks = await lockingManager.GetActiveLockInfoAsync(item, ct).ConfigureAwait(false); 
            return locks.Select(ali => ali.ToXml());
        };
    }
}

/// <summary>
/// Default implementation to describe the supported locks on a resource.
/// </summary>
/// <remarks>
/// This property implementation calls the
/// <see cref="ILockingManager.GetSupportedLocks"/> to determine the
/// supported locks.
/// </remarks>
/// <typeparam name="TEntry">
/// Store item or collection to which this DAV property applies.
/// </typeparam>
public sealed class DavSupportedLockDefault<TEntry> : DavSupportedLock<TEntry> where TEntry : IStoreItem
{
    /// <summary>
    /// Create an instance of the <see cref="DavSupportedLock{TEntry}"/>
    /// property that implements the property using the locking manager.
    /// </summary>
    public DavSupportedLockDefault(ILockingManager lockingManager)
    {
        Getter = item => lockingManager.GetSupportedLocks(item).Select(sl => sl.ToXml());
    }
}