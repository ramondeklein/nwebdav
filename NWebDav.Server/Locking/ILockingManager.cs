using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using NWebDav.Server.Stores;

namespace NWebDav.Server.Locking;

public readonly record struct LockResult(DavStatusCode Result, ActiveLock? Lock = null);

public interface ILockingManager
{
    Task<LockResult> LockAsync(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts, CancellationToken cancellationToken);
    Task<DavStatusCode> UnlockAsync(IStoreItem item, Uri token, CancellationToken cancellationToken);
    Task<LockResult> RefreshLockAsync(IStoreItem item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri, CancellationToken cancellationToken);

    Task<IEnumerable<ActiveLock>> GetActiveLockInfoAsync(IStoreItem item, CancellationToken cancellationToken);
    IEnumerable<LockEntry> GetSupportedLocks(IStoreItem item);

    Task<bool> IsLockedAsync(IStoreItem item, CancellationToken cancellationToken);
    Task<bool> HasLockAsync(IStoreItem item, Uri lockToken, CancellationToken cancellationToken);
}