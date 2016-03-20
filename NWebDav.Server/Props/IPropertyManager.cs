using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NWebDav.Server.Props
{
    public interface IPropertyManager
    {
        IEnumerable<PropertyInfo> Properties { get; }
        object GetProperty(IStoreCollectionEntry entry, XName name, bool skipExpensive = false);
        bool SetProperty(IStoreCollectionEntry entry, XName name, object value);
    }
}
