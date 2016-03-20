using System;
using System.Net;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;

namespace NWebDav.Server.Handlers
{
    [Verb("GET")]
    [Verb("HEAD")]
    public class GetAndHeadHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStoreResolver storeResolver)
        {
            // Obtain request and response
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var principal = httpListenerContext.User;

            // Determine if we are invoked as HEAD
            var head = request.HttpMethod == "HEAD";

            // Obtain the WebDAV collection
            var item = await storeResolver.GetItemAsync(request.Url, principal).ConfigureAwait(false);
            if (item == null)
            {
                // Set status to not found
                response.SendResponse(DavStatusCode.NotFound);
                return true;
            }

            // Set the response
            response.StatusCode = (int)DavStatusCode.OK;

            // Add non-expensive headers based on properties
            var propertyManager = item.PropertyManager;
            if (propertyManager != null)
            {
                // Add Last-Modified header
                var lastModifiedUtc = (string)propertyManager.GetProperty(item, WebDavNamespaces.DavNs + "getlastmodified", true);
                if (lastModifiedUtc != null)
                    response.AppendHeader("Last-Modified", lastModifiedUtc);

                // Add ETag
                var etag = (string)propertyManager.GetProperty(item, WebDavNamespaces.DavNs + "getetag", true);
                if (etag != null)
                    response.AppendHeader("Etag", etag);

                // Add type
                var contentType = (string)propertyManager.GetProperty(item, WebDavNamespaces.DavNs + "getcontenttype", true);
                if (contentType != null)
                    response.AppendHeader("Content-Type", contentType);

                // Add language
                var contentLanguage = (string)propertyManager.GetProperty(item, WebDavNamespaces.DavNs + "getcontentlanguage", true);
                if (contentLanguage != null)
                    response.AppendHeader("Content-Language", contentLanguage);
            }

            // HEAD method doesn't require the actual item data
            if (head)
            {
                // Close response
                response.Close();
            }
            else
            {
                // Stream the actual item
                using (var stream = item.GetReadableStream(principal))
                {
                    // Set the expected content length
                    try
                    {
                        if (stream.CanSeek)
                            response.ContentLength64 = stream.Length;
                    }
                    catch (NotSupportedException)
                    {
                        // If the content length is not supported, then we just skip it
                    }

                    await stream.CopyToAsync(response.OutputStream).ConfigureAwait(false);
                }
            }

            // Request is handled
            return true;
        }
    }
}
