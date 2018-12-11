using System;
using System.Collections.Generic;
using System.Threading;
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
        Task<LockResult> LockAsync(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts, CancellationToken cancellationToken);
        Task<DavStatusCode> UnlockAsync(IStoreItem item, Uri token, CancellationToken cancellationToken);
        Task<LockResult> RefreshLockAsync(IStoreItem item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri, CancellationToken cancellationToken);

        Task<IEnumerable<ActiveLock>> GetActiveLockInfoAsync(IStoreItem item, CancellationToken cancellationToken);
        Task<IEnumerable<LockEntry>> GetSupportedLocksAsync(IStoreItem item, CancellationToken cancellationToken);

        Task<bool> IsLockedAsync(IStoreItem item, CancellationToken cancellationToken);
        Task<bool> HasLockAsync(IStoreItem item, Uri lockToken, CancellationToken cancellationToken);
    }
}
