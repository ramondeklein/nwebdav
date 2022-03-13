using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

using NWebDav.Server.Stores;

namespace NWebDav.Server.Locking
{
    public struct LockResult
    {
        public DavStatusCode Result { get; }
        public ActiveLock? Lock { get; }

        public LockResult(DavStatusCode result, ActiveLock? @lock = null)
        {
            Result = result;
            Lock = @lock;
        }
    }

    // TODO: Call the locking methods from the handlers
    public interface ILockingManager
    {
        Task<LockResult> LockAsync(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts);
        Task<DavStatusCode> UnlockAsync(IStoreItem item, Uri token);

        /// <summary>
        /// Is called when a applications wants to maintain a lock on an <see cref="IStoreItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="IStoreItem"/>.</param>
        /// <param name="recursiveLock">Is true if the lock should also be set on all children.</param>
        /// <param name="timeouts">List of timeout values in seconds (<c>-1</c> if infinite).</param>
        /// <param name="lockTokenUri">The uri of the lock token.</param>
        /// <returns></returns>
        Task<LockResult> RefreshLockAsync(IStoreItem item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri);

        /// <summary>
        /// Used to describe the active locks on a resource, should be cheap, since it is added to every <see cref="IStoreItem"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        Task<IEnumerable<ActiveLock>> GetActiveLockInfoAsync(IStoreItem item);

        /// <summary>
        /// Used to describe the supported locks on a resource, should be cheap, since it is added to every <see cref="IStoreItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="IStoreItem"/>.</param>
        /// <returns></returns>
        Task<IEnumerable<LockEntry>> GetSupportedLocksAsync(IStoreItem item);


        /// <summary>
        /// Determines on deletion if the item is locked.
        /// </summary>
        /// <param name="item">The item which should be checked for locks.</param>
        /// <returns></returns>
        Task<bool> IsLockedAsync(IStoreItem item);

        /// <summary>
        /// In a delete scenario after it is determined the <see cref="IStoreItem"/> is locked, this method checks if the request lockToken matches the .
        /// </summary>
        /// <param name="item">The <see cref="IStoreItem"/>.</param>
        /// <param name="lockToken">The lock token provided by the request.</param>
        /// <returns></returns>
        bool HasLock(IStoreItem item, Uri lockToken);
    }
}
