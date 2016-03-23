using System;
using System.Diagnostics;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public interface IValidator
    {
        bool Validate(object value);
    }

    public interface IConverter<TSource>
    {
        object ToXml(TSource value);
        TSource FromXml(object value);
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
        public Func<TEntry, object> Getter { get; set; }
        public Func<TEntry, object, DavStatusCode> Setter { get; set; }
        public virtual IValidator Validator => DefaultValidator;
    }
}
