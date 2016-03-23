using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Locking
{
    public class InMemoryLockingManager : ILockingManager
    {
        private static readonly ScopeAndType[] SupportedLocks =
        {
            new ScopeAndType(LockScope.Exclusive, LockType.Write),
            new ScopeAndType(LockScope.Shared, LockType.Write)
        };

        public LockResult Lock(IStoreItem item, LockScope lockScope, LockType lockType, XElement owner, IEnumerable<int> timeouts)
        {
            // We can accept any
            return new LockResult();
        }

        public LockResult Unlock(IStoreItem item, string token)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ActiveLockInfo> GetActiveLockInfo(IStoreItem item)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ScopeAndType> GetSupportedLocks(IStoreItem item)
        {
            // We support both shared and exclusive locks for items and collections
            return SupportedLocks;
        }

        public bool IsLocked(IStoreItem item)
        {
            throw new NotImplementedException();
        }
    }
}
