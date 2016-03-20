using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NWebDav.Server
{
    public static class WebDavConstants
    {
        public static readonly XNamespace DavNs = "DAV:";
        public static readonly XNamespace Win32Ns = "urn:schemas-microsoft-com:";
        public static readonly XNamespace ReplNs = "http://schemas.microsoft.com/repl/";
        public static readonly XNamespace OfficeNs = "urn:schemas-microsoft-com:office:office";
    }
}
