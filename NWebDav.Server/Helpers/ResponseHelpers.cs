using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using NWebDav.Server.Http;
using NWebDav.Server.Logging;

namespace NWebDav.Server.Helpers
{
    public static class ResponseHelper
    {
        private static readonly ILogger s_log = LoggerFactory.CreateLogger(typeof(ResponseHelper));

        public static void SendResponse(this IHttpResponse response, DavStatusCode statusCode, string statusDescription = null)
        {
            response.Status = (int)statusCode;
            response.StatusDescription = statusDescription ?? DavStatusCodeHelper.GetStatusDescription(statusCode, "Unknown");
        }

        public static async Task SendResponseAsync(this IHttpResponse response, DavStatusCode statusCode, XDocument xDocument)
        {
            // Set the response
            response.SendResponse(statusCode);

            // Obtain the result as an XML document
            using (var ms = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(ms, new XmlWriterSettings
                {
                    OmitXmlDeclaration = false,
#if DEBUG
                    Indent = true,
#else
                    Indent = false,
#endif
                    Encoding = Encoding.UTF8,
                }))
                {
                    // Add the namespaces (Win7 WebDAV client requires them like this)
                    // TODO: We should do this a bit more flexible, but it will do for now
                    xDocument.Root.SetAttributeValue(XNamespace.Xmlns + WebDavNamespaces.DavNsShortcut, WebDavNamespaces.DavNs);
                    //xDocument.Root.SetAttributeValue(XNamespace.Xmlns + WebDavNamespaces.OfficeNsShortcut, WebDavNamespaces.OfficeNs);
                    //xDocument.Root.SetAttributeValue(XNamespace.Xmlns + WebDavNamespaces.ReplNsShortcut, WebDavNamespaces.ReplNs);
                    xDocument.Root.SetAttributeValue(XNamespace.Xmlns + WebDavNamespaces.Win32NsShortcut, WebDavNamespaces.Win32Ns);

                    // Write the XML document to the stream
                    xDocument.WriteTo(xmlWriter);
                }

                // Flush
                ms.Flush();

#if DEBUG
                // Reset stream and write the stream to the result
                ms.Seek(0, SeekOrigin.Begin);

                // Dump the XML document to the logging
                if (s_log.IsLogEnabled(LogLevel.Debug))
                {
                    var reader = new StreamReader(ms);
                    s_log.Log(LogLevel.Debug, reader.ReadToEnd());
                }
#endif

                // Set content type/length
                response.SetHeaderValue("Content-Type", "text/xml; charset=\"utf-8\"");
                response.SetHeaderValue("Content-Length", ms.Position.ToString(CultureInfo.InvariantCulture));

                // Reset stream and write the stream to the result
                ms.Seek(0, SeekOrigin.Begin);
                await ms.CopyToAsync(response.Stream).ConfigureAwait(false);
            }
        }
    }
}
