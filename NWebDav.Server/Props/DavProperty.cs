using System;
using System.Diagnostics;
using System.Xml.Linq;

using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public interface IValidator
    {
        bool Validate(object value);
    }

    public interface IConverter<TSource>
    {
        object ToXml(IHttpContext httpContext, TSource value);
        TSource FromXml(IHttpContext httpContext, object value);
    }

    [DebuggerDisplay("{Name}")]
    public abstract class DavProperty<TEntry> where TEntry : IStoreItem
    {
        private class DummyValidator : IValidator
        {
            public bool Validate(object value) => true;
        }

        private static IValidator DefaultValidator { get; } = new DummyValidator();

        public abstract XName Name { get; }
        public bool IsExpensive { get; set; }
        public Func<IHttpContext, TEntry, object> Getter { get; set; }
        public Func<IHttpContext, TEntry, object, DavStatusCode> Setter { get; set; }
        public virtual IValidator Validator => DefaultValidator;
    }
}
