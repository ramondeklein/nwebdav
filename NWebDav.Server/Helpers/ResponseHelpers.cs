﻿using Microsoft.Extensions.Logging;
using NWebDav.Server.Http;
using SecureFolderFS.Shared.Extensions;
using SecureFolderFS.Shared.Utils;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NWebDav.Server.Helpers
{
    /// <summary>
    /// Helper methods for <see cref="IHttpResponse"/> objects.
    /// </summary>
    public static class ResponseHelper
    {
        private static UTF8Encoding Utf8WithoutUnicodeByteOrderMark { get; } = new(false);

        /// <summary>
        /// Set status of the HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response that should be changed.</param>
        /// <param name="statusCode">WebDAV status code that should be set.</param>
        /// <param name="statusDescription">
        /// The human-readable WebDAV status description. If no status
        /// description is set, then the default status description is written. 
        /// </param>
        /// <remarks>
        /// Not all HTTP infrastructures allow to set the status description, so it should only be used for informational purposes.
        /// </remarks>
        public static void SetStatus(this IHttpResponse response, HttpStatusCode statusCode, string? statusDescription = null)
        {
            // Set the status code and description
            response.StatusCode = (int)statusCode;
            response.StatusDescription = (statusDescription?.Length ?? 0) == 0 ? statusCode.ToString() : statusDescription;
        }

        public static void SetStatus(this IHttpResponse response, IResult result)
        {
            // Set the status code and description
            response.StatusCode = (int)HttpStatusCode.Forbidden; // TODO(wd): Set appropriate status based on exception
            response.StatusDescription = result.GetMessage();
        }

        /// <summary>
        /// Send an HTTP response with an XML body content.
        /// </summary>
        /// <param name="response">The HTTP response that needs to be sent.</param>
        /// <param name="statusCode">WebDAV status code that should be set.</param>
        /// <param name="xDocument">XML document that should be sent as the body of the message.</param>
        /// <param name="logger">Used to trace warnings and debugging information.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        public static async Task SendResponseAsync(this IHttpResponse response, HttpStatusCode statusCode, XDocument xDocument, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            // Make sure an XML document is specified
            if (xDocument == null)
                throw new ArgumentNullException(nameof(xDocument));

            // Make sure the XML document has a root node
            if (xDocument.Root == null)
                throw new ArgumentException("The specified XML document doesn't have a root node", nameof(xDocument));

            // Set the response
            response.SetStatus(statusCode);

            // Obtain the result as an XML document
            await using var ms = new MemoryStream();
            await using (var xmlWriter = XmlWriter.Create(ms, new XmlWriterSettings()
            {
                OmitXmlDeclaration = false,
                Async = true,
#if DEBUG
                Indent = true,
#else
                Indent = false,
#endif
                Encoding = Utf8WithoutUnicodeByteOrderMark
            }))
            {
                // Add the namespaces (Win7 WebDAV client requires them like this)
                xDocument.Root.SetAttributeValue(XNamespace.Xmlns + WebDavNamespaces.DavNsPrefix, WebDavNamespaces.DavNs);
                xDocument.Root.SetAttributeValue(XNamespace.Xmlns + WebDavNamespaces.Win32NsPrefix, WebDavNamespaces.Win32Ns);

                // Write the XML document to the stream
                await xDocument.WriteToAsync(xmlWriter, cancellationToken).ConfigureAwait(false);
            }

            // Flush
            await ms.FlushAsync(cancellationToken).ConfigureAwait(false);
#if DEBUG
            // Dump the XML document to the logging
            if (logger?.IsEnabled(LogLevel.Debug) ?? false)
            {
                // Reset stream and write the stream to the result
                ms.Seek(0, SeekOrigin.Begin);

                using var reader = new StreamReader(ms, leaveOpen: true);
                logger.LogDebug(message: await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false));
            }
#endif
            // Set content type/length
            response.SetHeaderValue("Content-Type", "text/xml; charset=\"utf-8\"");
            response.SetHeaderValue("Content-Length", ms.Position.ToString(CultureInfo.InvariantCulture));

            // Reset stream and write the stream to the result
            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(response.OutputStream, cancellationToken).ConfigureAwait(false);
        }
    }
}
