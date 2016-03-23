using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using NWebDav.Server.Helpers;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Locking
{
    public struct ScopeAndType
    {
        public LockScope Scope { get; }
        public LockType Type { get; }

        public ScopeAndType(LockScope scope, LockType type)
        {
            Scope = scope;
            Type = type;
        }

        public XElement ToXml()
        {
            return new XElement(WebDavNamespaces.DavNs + "lockentry",
                new XElement(WebDavNamespaces.DavNs + "lockscope", EnumHelper.GetEnumValue(Scope)),
                new XElement(WebDavNamespaces.DavNs + "locktype", EnumHelper.GetEnumValue(Type)));
        }
    }

    public struct ActiveLockInfo
    {
        public LockType Type { get; }
        public LockScope Scope { get; }
        public int Depth { get; }
        public XElement Owner { get; }
        public int Timeout { get; }
        public Uri LockToken { get; }

        public ActiveLockInfo(LockType type, LockScope scope, int depth, XElement owner, int timeout, Uri lockToken)
        {
            Type = type;
            Scope = scope;
            Depth = depth;
            Owner = owner;
            Timeout = timeout;
            LockToken = lockToken;
        }

        public XElement ToXml()
        {
            return new XElement(WebDavNamespaces.DavNs + "lockentry",
                new XElement(WebDavNamespaces.DavNs + "locktype", EnumHelper.GetEnumValue(Type)),
                new XElement(WebDavNamespaces.DavNs + "lockscope", EnumHelper.GetEnumValue(Scope)),
                new XElement(WebDavNamespaces.DavNs + "depth", Depth == int.MaxValue ? "infinity" : Depth.ToString(CultureInfo.InvariantCulture),
                new XElement(WebDavNamespaces.DavNs + "owner", Owner),
                new XElement(WebDavNamespaces.DavNs + "timeout", Timeout == 1 ? "Infinite" : $"Second-{Timeout}"),
                new XElement(WebDavNamespaces.DavNs + "locktoken", new XElement(WebDavNamespaces.DavNs + "href", LockToken.AbsoluteUri))));
        }
    }

    public struct LockResult
    {
        public DavStatusCode Result { get; }
        public ActiveLockInfo? LockInfo { get; }

        public LockResult(DavStatusCode result, ActiveLockInfo? lockInfo = null)
        {
            Result = result;
            LockInfo = lockInfo;
        }
    }

    public interface ILockingManager
    {
        LockResult Lock(IStoreItem item, LockScope lockScope, LockType lockType, XElement owner, IEnumerable<int> timeouts);
        LockResult Unlock(IStoreItem item, string token);

        IEnumerable<ActiveLockInfo> GetActiveLockInfo(IStoreItem item);
        IEnumerable<ScopeAndType> GetSupportedLocks(IStoreItem item);

        bool IsLocked(IStoreItem item);
    }
}
