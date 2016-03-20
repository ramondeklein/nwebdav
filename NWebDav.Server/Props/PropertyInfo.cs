using System;
using System.Xml.Linq;

namespace NWebDav.Server.Props
{
    public struct PropertyInfo
    {
        public XName Name { get; }
        public bool IsExpensive { get; }

        public PropertyInfo(XName name, bool isExpensive)
        {
            Name = name;
            IsExpensive = isExpensive;
        }
    }
}
