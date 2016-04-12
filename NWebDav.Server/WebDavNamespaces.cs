using System;
using System.Xml.Linq;

namespace NWebDav.Server
{
    /// <summary>
    /// Defines all the XML namespaces that are typically used in WebDAV
    /// requests and responses.
    /// </summary>
    public static class WebDavNamespaces
    {
        /// <summary>
        /// Main DAV namespace (<c>DAV:</c>).
        /// </summary>
        public static readonly XNamespace DavNs = "DAV:";

        /// <summary>
        /// <para>
        /// Win32 extension namespace (<c>urn:schemas-microsoft-com:</c>). It's
        /// primary use is to add date/time and attributes to file-system based
        /// entries.
        /// </para>
        /// <para>
        /// More information can be found at
        /// <see href="https://msdn.microsoft.com/en-us/library/jj557737(v=office.12).aspx"/>.
        /// </para>
        /// </summary>
        public static readonly XNamespace Win32Ns = "urn:schemas-microsoft-com:";

        /// <summary>
        /// <para>
        /// The <c>http://schemas.microsoft.com/repl</c> namespace defines
        /// fields used for WebDAV replication.
        /// </para>
        /// <para>
        /// More information can be found at
        /// <see href="https://msdn.microsoft.com/en-us/library/ms875925(v=exchg.65).aspx"/>.
        /// </para>
        /// </summary>
        public static readonly XNamespace ReplNs = "http://schemas.microsoft.com/repl/";

        /// <summary>
        /// <para>
        /// The <c>urn:schemas-microsoft-com:office:office</c> namespace
        /// defines properties for use with Microsoft Office applications.
        /// </para>
        /// <para>
        /// More information can be found at
        /// <see href="https://msdn.microsoft.com/en-us/library/ms875215(v=exchg.65).aspx"/>.
        /// </para>
        /// </summary>
        public static readonly XNamespace OfficeNs = "urn:schemas-microsoft-com:office:office";
    }
}
