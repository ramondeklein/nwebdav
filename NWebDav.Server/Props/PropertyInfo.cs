using System;
using System.Xml.Linq;

namespace NWebDav.Server.Props
{
    public struct PropertyInfo
    {
        public XName Name { get; }
        public bool IsExpensive { get; }        // TODO: Don't use the term 'Expensive'

        public PropertyInfo(XName name, bool isExpensive)
        {
            Name = name;
            IsExpensive = isExpensive;
        }
    }
}
