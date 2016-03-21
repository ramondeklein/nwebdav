using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NWebDav.Server.Props
{
    public interface IPropertyManager
    {
        IEnumerable<PropertyInfo> Properties { get; }
        object GetProperty(IStoreItem entry, XName name, bool skipExpensive = false);
        bool SetProperty(IStoreItem entry, XName name, object value);
    }
}
