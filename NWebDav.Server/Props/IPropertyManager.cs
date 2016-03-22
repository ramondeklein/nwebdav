using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NWebDav.Server.Props
{
    public interface IPropertyManager
    {
        IEnumerable<PropertyInfo> Properties { get; }
        object GetProperty(IStoreItem item, XName name, bool skipExpensive = false);
        DavStatusCode SetProperty(IStoreItem item, XName name, object value);
    }
}
