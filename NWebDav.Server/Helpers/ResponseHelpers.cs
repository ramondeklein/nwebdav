using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NWebDav.Server.Helpers
{
    public static class ResponseHelper
    {
        public static void SendResponse(this HttpListenerResponse response, DavStatusCode statusCode, string statusDescription = null)
        {
            response.StatusCode = (int)statusCode;
            response.StatusDescription = statusDescription ?? GetStatusDescription(statusCode);
            response.Close();
        }

        public static async Task SendResponseAsync(this HttpListenerResponse response, DavStatusCode statusCode, XDocument xDocument)
        {
            // Set the response
            response.StatusCode = (int)statusCode;
            response.StatusDescription = GetStatusDescription(statusCode);

            // Obtain the result as an XML document
            using (var ms = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(ms, new XmlWriterSettings()
                {
                    OmitXmlDeclaration = false,
#if DEBUG
                    Indent = true,
#else
                    Indent = false,
#endif
                    Encoding = Encoding.UTF8
                }))
                {
                    // Write the XML document to the stream
                    xDocument.WriteTo(xmlWriter);
                }

                //Flush
                ms.Flush();

                // Set content type/length
                response.ContentType = " text/xml; charset=\"utf-8\"";
                response.ContentLength64 = ms.Position;

                // Reset stream and write the stream to the result
                ms.Seek(0, SeekOrigin.Begin);
                await ms.CopyToAsync(response.OutputStream);
            }
        }

        public static string GetStatusDescription(DavStatusCode davStatusCode)
        {
            // Obtain the member information
            var memberInfo = typeof(DavStatusCode).GetMember(davStatusCode.ToString()).FirstOrDefault();
            if (memberInfo == null)
                return "Unknown";

            var descriptionAttribute = memberInfo.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();
            return descriptionAttribute?.Description;
        }
    }
}
