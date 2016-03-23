using System;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public class DavChildCount<TEntry> : DavInt32<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "childcount";
    }

    public class DavIsCollection<TEntry> : DavBoolean<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "iscollection";
    }

    public class DavIsFolder<TEntry> : DavBoolean<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "isfolder";
    }

    public class DavIsHidden<TEntry> : DavBoolean<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "ishidden";
    }

    public class DavHasSubs<TEntry> : DavBoolean<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "hassubs";
    }

    public class DavNoSubs<TEntry> : DavBoolean<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "nosubs";
    }

    public class DavObjectCount<TEntry> : DavInt32<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "objectcount";
    }

    public class DavVisibleCount<TEntry> : DavInt32<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "visiblecount";
    }
}
