using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NWebDav.Server.Stores;

namespace NWebDav.Server.Locking
{
    // TODO: Add automatic lock expiration
    // TODO: Add support for recursive locks
    public class InMemoryLockingManager : ILockingManager
    {
        #region Private helper classes

        private class ItemLockInfo
        {
            public Guid Token { get; }
            public IStoreItem Item { get; }
            public LockType Type { get; }
            public LockScope Scope { get; set; }
            public bool Recursive { get; set; }
            public XElement Owner { get; set; }
            public int Timeout { get; set; }
            public DateTime? Expires { get; private set; }

            public ItemLockInfo(IStoreItem item, LockType lockType, LockScope lockScope, bool recursive, XElement owner, int timeout)
            {
                Token = Guid.NewGuid();
                Item = item;
                Type = lockType;
                Scope = lockScope;
                Recursive = recursive;
                Owner = owner;
                Timeout = timeout;

                RefreshExpiration();
            }

            public void RefreshExpiration()
            {
                Expires = Timeout >= 0 ? (DateTime?)DateTime.UtcNow.AddSeconds(Timeout) : null;
            }
        }

        private class ItemLockList : List<ItemLockInfo>
        {
        }

        private class ItemLockTypeDictionary : Dictionary<LockType, ItemLockList>
        {
        }

        #endregion

        #region Private constants and fields

        private const string TokenScheme = "opaquelocktoken";

        private readonly IDictionary<IStoreItem, ItemLockTypeDictionary> _itemLocks = new Dictionary<IStoreItem, ItemLockTypeDictionary>();

        private static readonly ScopeAndType[] s_supportedLocks =
        {
            new ScopeAndType(LockScope.Exclusive, LockType.Write),
            new ScopeAndType(LockScope.Shared, LockType.Write)
        };

        #endregion

        #region Public methods

        public LockResult Lock(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, bool recursive, IEnumerable<int> timeouts, Guid? existingToken = null)
        {
            // Determine the expiration based on the first time-out
            var timeout = timeouts.Cast<int?>().FirstOrDefault();

            lock (_itemLocks)
            {
                // Make sure the item is in the dictionary
                ItemLockTypeDictionary itemLockTypeDictionary;
                if (!_itemLocks.TryGetValue(item, out itemLockTypeDictionary))
                    _itemLocks.Add(item, itemLockTypeDictionary = new ItemLockTypeDictionary());

                // Make sure there is already a lock-list for this type
                ItemLockList itemLockList;
                if (!itemLockTypeDictionary.TryGetValue(lockType, out itemLockList))
                {
                    // Create a new lock-list
                    itemLockTypeDictionary.Add(lockType, itemLockList = new ItemLockList());
                }
                else
                {
                    // Determine our existing lock information
                    var existingLockInfo = existingToken != null ? itemLockList.FirstOrDefault(il => il.Token == existingToken.Value) : null;

                    // Check if there is already an exclusive lock
                    if (itemLockList.Any(l => l.Scope == LockScope.Exclusive))
                    {
                        // If our lock is not the exclusive lock, then the lock cannot be granted
                        if (existingLockInfo == null || existingLockInfo.Scope != LockScope.Exclusive)
                            return new LockResult(DavStatusCode.Locked);
                    }

                    // Update our lock
                    if (existingLockInfo != null)
                    {
                        // Update the timeout value
                        if (timeout.HasValue)
                            existingLockInfo.Timeout = timeout.Value;

                        // Refresh the expiration
                        existingLockInfo.RefreshExpiration();

                        // Our lock has been updated
                        return new LockResult(DavStatusCode.Ok, GetActiveLockInfo(existingLockInfo));
                    }
                }

                // Create the lock info object
                var itemLockInfo = new ItemLockInfo(item, lockType, lockScope, recursive, owner, timeout ?? -1);

                // Add the lock
                itemLockList.Add(itemLockInfo);

                // Return the active lock
                return new LockResult(DavStatusCode.Ok, GetActiveLockInfo(itemLockInfo));
            }
        }

        public DavStatusCode Unlock(IStoreItem item, Uri lockTokenUri)
        {
            // Determine the actual lock token
            var lockToken = GetTokenFromLockToken(lockTokenUri);
            if (lockToken == null)
                return DavStatusCode.PreconditionFailed;

            lock (_itemLocks)
            {
                // Make sure the item is in the dictionary
                ItemLockTypeDictionary itemLockTypeDictionary;
                if (!_itemLocks.TryGetValue(item, out itemLockTypeDictionary))
                    return DavStatusCode.PreconditionFailed;

                // Scan both the dictionaries for the token
                foreach (var kv in itemLockTypeDictionary)
                {
                    var itemLockList = kv.Value;

                    // Remove this lock from the list
                    for (var i = 0; i < itemLockList.Count; ++i)
                    {
                        if (itemLockList[i].Token == lockToken.Value)
                        {
                            // Remove the item
                            itemLockList.RemoveAt(i);

                            // Check if there are any locks left for this type
                            if (!itemLockList.Any())
                            {
                                // Remove the type
                                itemLockTypeDictionary.Remove(kv.Key);

                                // Check if there are any types left
                                if (!itemLockTypeDictionary.Any())
                                    _itemLocks.Remove(item);
                            }

                            // Lock has been removed
                            return DavStatusCode.NoContent;
                        }
                    }
                }
            }

            // Item cannot be unlocked (token cannot be found)
            return DavStatusCode.PreconditionFailed;
        }

        public IEnumerable<ActiveLockInfo> GetActiveLockInfo(IStoreItem item)
        {
            lock (_itemLocks)
            {
                // Make sure the item is in the dictionary
                ItemLockTypeDictionary itemLockTypeDictionary;
                if (!_itemLocks.TryGetValue(item, out itemLockTypeDictionary))
                    return new ActiveLockInfo[0];

                // Determine current date
                var utcNow = DateTime.UtcNow;

                // Return all non-expired locks
                return itemLockTypeDictionary.SelectMany(kv => kv.Value).Where(l => !l.Expires.HasValue || utcNow <= l.Expires.Value).Select(GetActiveLockInfo).ToList();
            }
        }

        public IEnumerable<ScopeAndType> GetSupportedLocks(IStoreItem item)
        {
            // We support both shared and exclusive locks for items and collections
            return s_supportedLocks;
        }

        public bool HasLock(IStoreItem item, LockType lockType, Uri lockTokenUri)
        {
            // Determine the actual lock token
            var lockToken = GetTokenFromLockToken(lockTokenUri);
            if (lockToken == null)
                return false;

            lock (_itemLocks)
            {
                // Make sure the item is in the dictionary
                ItemLockTypeDictionary itemLockTypeDictionary;
                if (!_itemLocks.TryGetValue(item, out itemLockTypeDictionary))
                    return false;

                // Obtain the item locks for the specified type
                ItemLockList itemLockList;
                if (!itemLockTypeDictionary.TryGetValue(lockType, out itemLockList))
                    return false;

                // Determine this lock
                var itemLock = itemLockList.FirstOrDefault(l => l.Token == lockToken);
                if (itemLock == null)
                    return false;

                // Check if the lock is still valid
                if (itemLock.Expires.HasValue && DateTime.UtcNow > itemLock.Expires.Value)
                    return false;

                // Lock is valid
                return true;
            }
        }

        #endregion

        #region Private helper methods

        private ActiveLockInfo GetActiveLockInfo(ItemLockInfo itemLockInfo)
        {
            return new ActiveLockInfo(itemLockInfo.Type, itemLockInfo.Scope, itemLockInfo.Recursive ? int.MaxValue : 0, itemLockInfo.Owner, itemLockInfo.Timeout, new Uri($"{TokenScheme}:{itemLockInfo.Token:D}"));
        }

        private Guid? GetTokenFromLockToken(Uri lockTokenUri)
        {
            // We should always use opaquetokens
            if (lockTokenUri.Scheme != TokenScheme)
                return null;

            // Parse the token
            Guid lockToken;
            if (!Guid.TryParse(lockTokenUri.LocalPath, out lockToken))
                return null;

            // Return the token
            return lockToken;
        }

        #endregion
    }
}
