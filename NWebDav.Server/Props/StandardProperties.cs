using System;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public class DavCreationDate<TEntry> : DavIso8601Date<TEntry> where TEntry : IStoreItem
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

    public class DavSource<TEntry> : DavXElement<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.DavNs + "source";
    }
}
