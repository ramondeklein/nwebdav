using System;
using System.Diagnostics;
using System.Xml.Linq;

using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    [DebuggerDisplay("{Name}")]
    public abstract class DavProperty<TEntry> where TEntry : IStoreItem
    {
        public abstract XName Name { get; }

        public Func<IHttpContext, TEntry, object> Getter { get; set; }
        public Func<IHttpContext, TEntry, object, DavStatusCode> Setter { get; set; }

        public bool IsExpensive { get; set; }
    }
}
