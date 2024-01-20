using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Locking;

public abstract class LockingManager : ILockingManager
{
    public Task<LockResult> LockAsync(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Lock(item, lockType, lockScope, owner, lockRootUri, recursiveLock, timeouts));
    }
        
    public Task<DavStatusCode> UnlockAsync(IStoreItem item, Uri token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Unlock(item, token));
    }
    public Task<LockResult> RefreshLockAsync(IStoreItem item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(RefreshLock(item, recursiveLock, timeouts, lockTokenUri));
    }

    public Task<IEnumerable<ActiveLock>> GetActiveLockInfoAsync(IStoreItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetActiveLockInfo(item));
    }

    IEnumerable<LockEntry> ILockingManager.GetSupportedLocks(IStoreItem item)
    {
        return GetSupportedLocks(item);
    }

    public Task<bool> IsLockedAsync(IStoreItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(IsLocked(item));
    }

    public Task<bool> HasLockAsync(IStoreItem item, Uri lockToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(HasLock(item, lockToken));
    }

    protected abstract LockResult Lock(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts);
    protected abstract DavStatusCode Unlock(IStoreItem item, Uri token);
    protected abstract LockResult RefreshLock(IStoreItem item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri);

    protected abstract IEnumerable<ActiveLock> GetActiveLockInfo(IStoreItem item);
    public abstract IEnumerable<LockEntry> GetSupportedLocks(IStoreItem item);

    protected abstract bool IsLocked(IStoreItem item);
    protected abstract bool HasLock(IStoreItem item, Uri lockToken);

}