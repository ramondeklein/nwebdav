using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public interface IPropertyManager
    {
        IEnumerable<PropertyInfo> Properties { get; }
        object GetProperty(IPrincipal principal, IStoreItem item, XName name, bool skipExpensive = false);
        DavStatusCode SetProperty(IPrincipal principal, IStoreItem item, XName name, object value);
    }
}
