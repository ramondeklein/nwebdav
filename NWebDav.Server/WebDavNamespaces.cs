using System;
using System.Xml.Linq;

namespace NWebDav.Server
{
    public static class WebDavNamespaces
    {
        public static readonly XNamespace DavNs = "DAV:";

        // See https://msdn.microsoft.com/en-us/library/jj557737(v=office.12).aspx
        public static readonly XNamespace Win32Ns = "urn:schemas-microsoft-com:";
        public static readonly XNamespace ReplNs = "http://schemas.microsoft.com/repl/";
        public static readonly XNamespace OfficeNs = "urn:schemas-microsoft-com:office:office";
    }
}
