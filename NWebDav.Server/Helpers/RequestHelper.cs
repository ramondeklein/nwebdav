﻿using Microsoft.Extensions.Logging;
using NWebDav.Server.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NWebDav.Server.Helpers
{
    /// <summary>
    /// Split URI consisting of a collection URI and a name string.
    /// </summary>
    public class SplitUri
    {
        /// <summary>
        /// Collection URI that holds the collection/document.
        /// </summary>
        public Uri CollectionUri { get; set; }

        /// <summary>
        /// Name of the collection/document within its container collection.
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Range
    /// </summary>
    public class Range
    {
        /// <summary>
        /// Optional start value.
        /// </summary>
        public long? Start { get; set; }

        /// <summary>
        /// Optional end value.
        /// </summary>
        public long? End { get; set; }

        /// <summary>
        /// Optional conditional date/time.
        /// </summary>
        public DateTime If {get; set; }
    }

    /// <summary>
    /// Helper methods for <see cref="IHttpRequest"/> objects.
    /// </summary>
    public static class RequestHelper
    {
        private static readonly Regex s_rangeRegex = new Regex("bytes\\=(?<start>[0-9]*)-(?<end>[0-9]*)");

        /// <summary>
        /// Split an URI into a collection and name part.
        /// </summary>
        /// <param name="uri">URI that should be split.</param>
        /// <returns>
        /// Split URI in a collection URI and a name string.
        /// </returns>
        public static SplitUri SplitUri(Uri uri)
        {
            // Strip a trailing slash
            var trimmedUri = uri.AbsoluteUri;
            if (trimmedUri.EndsWith("/"))
                trimmedUri = trimmedUri.Substring(0, trimmedUri.Length - 1);

            // Determine the offset of the name
            var slashOffset = trimmedUri.LastIndexOf('/');
            if (slashOffset == -1)
                return null;

            // Separate name from path
            return new SplitUri
            {
                CollectionUri = new Uri(trimmedUri.Substring(0, slashOffset)),
                Name = Uri.UnescapeDataString(trimmedUri.Substring(slashOffset + 1))
            };
        }

        /// <summary>
        /// Obtain the destination uri from the request.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>
        /// Destination for this HTTP request (or <see langword="null"/> if no
        /// destination is specified).
        /// </returns>
        public static Uri GetDestinationUri(this IHttpRequest request)
        {
            // Obtain the destination
            var destinationHeader = request.GetHeaderValue("Destination");
            if (destinationHeader == null)
                return null;

            // Create the destination URI
            return destinationHeader.StartsWith("/") ? new Uri(request.Url, destinationHeader) : new Uri(destinationHeader);
        }        

        /// <summary>
        /// Obtain the depth value from the request.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>
        /// Depth of the HTTP request (<c>int.MaxValue</c> if infinity).
        /// </returns>
        /// <remarks>
        /// If the Depth header is not set, then the specification specifies
        /// that it should be interpreted as infinity.
        /// </remarks>
        public static int GetDepth(this IHttpRequest request)
        {
            // Obtain the depth header (no header means infinity)
            var depthHeader = request.GetHeaderValue("Depth");
            if (depthHeader == null || depthHeader == "infinity")
                return int.MaxValue;

            // Determined depth
            if (!int.TryParse(depthHeader, out var depth))
                return int.MaxValue;

            // Return depth
            return depth;
        }

        /// <summary>
        /// Obtain the overwrite value from the request.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>
        /// Flag indicating whether or not to overwrite the destination
        /// if it already exists.
        /// </returns>
        /// <remarks>
        /// If the Overwrite header is not set, then the specification
        /// specifies that it should be interpreted as 
        /// <see langwordk="true"/>.
        /// </remarks>
        public static bool GetOverwrite(this IHttpRequest request)
        {
            // Get the Overwrite header
            var overwriteHeader = request.GetHeaderValue("Overwrite") ?? "T";

            // It should be set to "T" (true) or "F" (false)
            return overwriteHeader.ToUpperInvariant() == "T";
        }

        /// <summary>
        /// Obtain the list of timeout values from the request.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>
        /// List of timeout values in seconds (<c>-1</c> if infinite).
        /// </returns>
        /// <remarks>
        /// If the Timeout header is not set, then <see langword="null"/> is
        /// returned.
        /// </remarks>
        public static IList<int> GetTimeouts(this IHttpRequest request)
        {
            // Get the value of the timeout header as a string
            var timeoutHeader = request.GetHeaderValue("Timeout");
            if (string.IsNullOrEmpty(timeoutHeader))
                return null;

            // Return each item
            int ParseTimeout(string t)
            {
                // Check for 'infinite'
                if (t == "Infinite")
                    return -1;

                // Parse the number of seconds
                if (!t.StartsWith("Second-") || !int.TryParse(t.Substring(7), out var timeout))
                    return 0;
                return timeout;
            }

            // Return the timeout values
            return timeoutHeader.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(ParseTimeout).Where(t => t != 0).ToArray();
        }

        /// <summary>
        /// Obtain the lock-token URI from the request.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>
        /// Lock token URI (<see langword="null"/> if not set).
        /// </returns>
        public static Uri GetLockToken(this IHttpRequest request)
        {
            // Get the value of the lock-token header as a string
            var lockTokenHeader = request.GetHeaderValue("Lock-Token");
            if (string.IsNullOrEmpty(lockTokenHeader))
                return null;

            // Strip the brackets from the header
            if (!lockTokenHeader.StartsWith("<") || !lockTokenHeader.EndsWith(">"))
                return null;

            // Create an Uri of the intermediate part
            return new Uri(lockTokenHeader.Substring(1, lockTokenHeader.Length - 2), UriKind.Absolute);
        }

        /// <summary>
        /// Obtain the if-lock-token URI from the request.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>
        /// If lock token URI (<see langword="null"/> if not set).
        /// </returns>
        public static Uri GetIfLockToken(this IHttpRequest request)
        {
            // Get the value of the lock-token header as a string
            var lockTokenHeader = request.GetHeaderValue("If");
            if (string.IsNullOrEmpty(lockTokenHeader))
                return null;

            // Strip the brackets from the header
            if (!lockTokenHeader.StartsWith("(<") || !lockTokenHeader.EndsWith(">)"))
                return null;

            // Create an Uri of the intermediate part
            return new Uri(lockTokenHeader.Substring(2, lockTokenHeader.Length - 4), UriKind.Absolute);
        }

        /// <summary>
        /// Obtain the range value from the request.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>
        /// Range value (start/end) with an option if condition.
        /// </returns>
        public static Range GetRange(this IHttpRequest request)
        {
            // Get the value of the range header as a string
            var rangeHeader = request.GetHeaderValue("Range");
            if (string.IsNullOrEmpty(rangeHeader))
                return null;

            // We only support the bytes=<start>-<end> format
            var match = s_rangeRegex.Match(rangeHeader);
            if (!match.Success)
                throw new FormatException($"Illegal format for range header: {rangeHeader}");

            // Obtain the start and end
            var startText = match.Groups["start"].Value;
            var endText = match.Groups["end"].Value;
            var range = new Range
            {
                Start = !string.IsNullOrEmpty(startText) ? (long?)long.Parse(startText) : null,
                End = !string.IsNullOrEmpty(endText) ? (long?)long.Parse(endText ) : null
            };

            // Check if we also have an If-Range
            var ifRangeHeader = request.GetHeaderValue("If-Range");
            if (ifRangeHeader != null)
            {
                // Attempt to parse the date. If we don't understand the If-Range
                // then we need to return the entire file, so we will act as if no
                // range was specified at all.
                if (!DateTime.TryParse(ifRangeHeader, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return null;

                // Use the date for the 'If'
                range.If = dt;
            }

            // Return the range
            return range;
        }

        /// <summary>
        /// Load an XML document from the HTTP request body.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>
        /// XML document that represents the body content (or 
        /// <see langword="null"/> if no body content is specified).
        /// </returns>

        // The async version of XDocument.LoadAsync has become available with .NET Standard 2.1
        // and is required for modern Kestrel versions that require all reads to be done async.
        // Older versions use a synchronous version and have the additional penalty of using
        // async, but if performance is an ultimate goal, then don't use WebDAV and you should
        // be upgrading to .NET Core anyway :-) The other option is to put the burden on all
        // the callers of this method, which I prefer to avoid.
        public static async Task<XDocument> LoadXmlDocumentAsync(this IHttpRequest request, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            // If there is no input stream, then there is no XML document
            if (request.InputStream == null || request.InputStream == Stream.Null)
                return null;

            // Return null if no content has been specified
            var contentLengthString = request.GetHeaderValue("Content-Length");
            if (contentLengthString != null)
            {
                if (!int.TryParse(contentLengthString, out var contentLength) || contentLength == 0)
                    return null;
            }

            // Return null if no stream is available
            if (request.InputStream == null || request.InputStream == Stream.Null)
                return null;

            // Obtain an XML document from the stream
            var xDocument = await XDocument.LoadAsync(request.InputStream, LoadOptions.None, cancellationToken);

#if DEBUG
            // Dump the XML document to the logging
            if (xDocument.Root != null && (logger?.IsEnabled(LogLevel.Debug) ?? false))
            {
                // Format the XML document as an in-memory text representation
                await using var ms = new MemoryStream();
                await using (var xmlWriter = System.Xml.XmlWriter.Create(ms, new System.Xml.XmlWriterSettings()
                {
                    OmitXmlDeclaration = false,
                    Async = true,
                    Indent = true,
                    Encoding = System.Text.Encoding.UTF8,
                }))
                {
                    // Write the XML document to the stream
                    await xDocument.WriteToAsync(xmlWriter, cancellationToken).ConfigureAwait(false);
                }

                // Flush
                await ms.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Reset stream and write the stream to the result
                ms.Seek(0, SeekOrigin.Begin);

                // Log the XML text to the logging
                var reader = new StreamReader(ms);
                logger.LogDebug(message: await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false));
            }
#endif
            // Return the XML document
            return xDocument;
        }
    }
}
