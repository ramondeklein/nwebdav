using System.Linq;

using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    /// <summary>
    /// Default implementation to describe the active locks on a resource.
    /// </summary>
    /// <remarks>
    /// This property implementation calls the
    /// <see cref="NWebDav.Server.Locking.ILockingManager.GetActiveLockInfoAsync"/>
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
        /// <see cref="NWebDav.Server.Locking.ILockingManager.GetActiveLockInfoAsync"/> 
        /// method of the item's locking manager.
        /// </summary>
        public DavLockDiscoveryDefault()
        {
            GetterAsync = async (httpContext, item) => (await item.LockingManager.GetActiveLockInfoAsync(item).ConfigureAwait(false))
                .Select(ali => ali.ToXml());
        }
    }

    /// <summary>
    /// Default implementation to describe the supported locks on a resource.
    /// </summary>
    /// <remarks>
    /// This property implementation calls the
    /// <see cref="NWebDav.Server.Locking.ILockingManager.GetSupportedLocksAsync"/>
    /// of the item's <see cref="IStoreItem.LockingManager"/> to determine the
    /// supported locks.
    /// </remarks>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public sealed class DavSupportedLockDefault<TEntry> : DavSupportedLock<TEntry> where TEntry : IStoreItem
    {
        /// <summary>
        /// Create an instance of the <see cref="DavSupportedLock{TEntry}"/>
        /// property that implements the property using the
        /// <see cref="NWebDav.Server.Locking.ILockingManager.GetSupportedLocksAsync"/>
        /// method of the item's locking manager.
        /// </summary>
        public DavSupportedLockDefault()
        {
            GetterAsync = async (httpContext, item) => (await item.LockingManager.GetSupportedLocksAsync(item).ConfigureAwait(false))
                .Select(sl => sl.ToXml());
        }
    }
}
