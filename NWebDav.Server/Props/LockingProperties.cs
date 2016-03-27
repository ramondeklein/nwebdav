using System;
using System.Linq;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public class DavLockDiscovery<TEntry> : DavXElementArray<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "lockdiscovery";
    }

    public class DavLockDiscoveryDefault<TEntry> : DavLockDiscovery<TEntry> where TEntry : IStoreItem
    {
        public DavLockDiscoveryDefault()
        {
            Getter = item => item.LockingManager.GetActiveLockInfo(item).Select(ali => ali.ToXml());
        }
    }

    public class DavSupportedLock<TEntry> : DavXElementArray<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "supportedlock";
    }

    public class DavSupportedLockDefault<TEntry> : DavSupportedLock<TEntry> where TEntry : IStoreItem
    {
        public DavSupportedLockDefault()
        {
            Getter = item => item.LockingManager.GetSupportedLocks(item).Select(sl => sl.ToXml());
        }
    }
}
