using System;
using System.Diagnostics;
using System.Xml.Linq;

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
    public abstract class DavProperty<TEntry> where TEntry : IStoreCollectionEntry
    {
        private class DummyValidator : IValidator
        {
            public bool Validate(object value) => true;
        }

        private static IValidator DefaultValidator { get; } = new DummyValidator();

        public abstract XName Name { get; }
        public bool IsExpensive { get; set; }
        public Func<TEntry, object> Getter { get; set; }
        public Func<TEntry, object, bool> Setter { get; set; }
        public virtual IValidator Validator => DefaultValidator;
    }
}
