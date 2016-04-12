using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using NWebDav.Server.Helpers;
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
        LockResult Lock(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, bool recursiveLock, IEnumerable<int> timeouts, Guid? existingToken = null);
        DavStatusCode Unlock(IStoreItem item, Uri token);

        IEnumerable<ActiveLock> GetActiveLockInfo(IStoreItem item);
        IEnumerable<LockEntry> GetSupportedLocks(IStoreItem item);

        bool HasLock(IStoreItem item, LockType lockType, Uri lockToken);
    }
}
