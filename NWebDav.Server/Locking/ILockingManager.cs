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
                new XElement(WebDavNamespaces.DavNs + "lockscope", DavStatusCodeHelper.GetStatusDescription(Scope)),
                new XElement(WebDavNamespaces.DavNs + "locktype", DavStatusCodeHelper.GetStatusDescription(Type)));
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
            return new XElement(WebDavNamespaces.DavNs + "activelock",
                new XElement(WebDavNamespaces.DavNs + "locktype", new XElement(WebDavNamespaces.DavNs + XmlHelper.GetXmlValue(Type))),
                new XElement(WebDavNamespaces.DavNs + "lockscope", new XElement(WebDavNamespaces.DavNs + XmlHelper.GetXmlValue(Scope))),
                new XElement(WebDavNamespaces.DavNs + "depth", Depth == int.MaxValue ? "infinity" : Depth.ToString(CultureInfo.InvariantCulture)),
                new XElement(WebDavNamespaces.DavNs + "owner", Owner),
                new XElement(WebDavNamespaces.DavNs + "timeout", Timeout == -1 ? "Infinite" : "Second-" + Timeout.ToString(CultureInfo.InvariantCulture)),
                new XElement(WebDavNamespaces.DavNs + "locktoken", new XElement(WebDavNamespaces.DavNs + "href", LockToken.AbsoluteUri)));
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

    // TODO: Call the locking methods from the handlers
    public interface ILockingManager
    {
        LockResult Lock(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, bool recursiveLock, IEnumerable<int> timeouts, Guid? existingToken = null);
        DavStatusCode Unlock(IStoreItem item, Uri token);

        IEnumerable<ActiveLockInfo> GetActiveLockInfo(IStoreItem item);
        IEnumerable<ScopeAndType> GetSupportedLocks(IStoreItem item);

        bool HasLock(IStoreItem item, LockType lockType, Uri lockToken);
    }
}
