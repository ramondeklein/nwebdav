using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Xml.Linq;
using System.Xml.Schema;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public class DavCreationDate<TEntry> : DavRfc1123Date<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "creationdate";
    }

    public class DavDisplayName<TEntry> : DavString<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "displayname";
    }

    public class DavGetContentLanguage<TEntry> : DavString<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "getcontentlanguage";
    }

    public class DavGetContentLength<TEntry> : DavInt64<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "getcontentlength";
    }

    public class DavGetContentType<TEntry> : DavString<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "getcontenttype";
    }

    public class DavGetEtag<TEntry> : DavString<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "getetag";
    }

    public class DavGetLastModified<TEntry> : DavRfc1123Date<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "getlastmodified";
    }

    public class DavGetResourceType<TEntry> : DavXElement<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "resourcetype";
    }

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
