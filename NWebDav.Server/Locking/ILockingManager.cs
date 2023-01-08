using NWebDav.Server.Stores;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;

namespace NWebDav.Server.Locking
{
    public struct LockResult
    {
        public HttpStatusCode Result { get; }
        public ActiveLock? Lock { get; }

        public LockResult(HttpStatusCode result, ActiveLock? @lock = null)
        {
            Result = result;
            Lock = @lock;
        }
    }

    // TODO: Call the locking methods from the handlers
    public interface ILockingManager
    {
        LockResult Lock(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts);
        HttpStatusCode Unlock(IStoreItem item, Uri token);
        LockResult RefreshLock(IStoreItem item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri);

        IEnumerable<ActiveLock> GetActiveLockInfo(IStoreItem item);
        IEnumerable<LockEntry> GetSupportedLocks(IStoreItem item);

        bool IsLocked(IStoreItem item);
        bool HasLock(IStoreItem item, Uri lockToken);
    }
}
