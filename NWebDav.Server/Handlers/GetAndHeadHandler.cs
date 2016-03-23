using System;
using System.Net;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    [Verb("GET")]
    [Verb("HEAD")]
    public class GetAndHeadHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStore store)
        {
            // Obtain request and response
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var principal = httpListenerContext.User;

            // Determine if we are invoked as HEAD
            var head = request.HttpMethod == "HEAD";

            // Obtain the WebDAV collection
            var entry = await store.GetItemAsync(request.Url, principal).ConfigureAwait(false);
            if (entry == null)
            {
                // Set status to not found
                response.SendResponse(DavStatusCode.NotFound);
                return true;
            }

            // Add non-expensive headers based on properties
            var propertyManager = entry.PropertyManager;
            if (propertyManager != null)
            {
                // Add Last-Modified header
                var lastModifiedUtc = (string)propertyManager.GetProperty(entry, WebDavNamespaces.DavNs + "getlastmodified", true);
                if (lastModifiedUtc != null)
                    response.AppendHeader("Last-Modified", lastModifiedUtc);

                // Add ETag
                var etag = (string)propertyManager.GetProperty(entry, WebDavNamespaces.DavNs + "getetag", true);
                if (etag != null)
                    response.AppendHeader("Etag", etag);

                // Add type
                var contentType = (string)propertyManager.GetProperty(entry, WebDavNamespaces.DavNs + "getcontenttype", true);
                if (contentType != null)
                    response.AppendHeader("Content-Type", contentType);

                // Add language
                var contentLanguage = (string)propertyManager.GetProperty(entry, WebDavNamespaces.DavNs + "getcontentlanguage", true);
                if (contentLanguage != null)
                    response.AppendHeader("Content-Language", contentLanguage);
            }

            // Set the response
            response.StatusCode = (int)DavStatusCode.OK;

            // HEAD method doesn't require the actual item data
            if (!head)
            {
                // Stream the actual entry
                using (var stream = entry.GetReadableStream(principal))
                {
                    if (stream != null)
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

                        // Copy the entire item
                        await stream.CopyToAsync(response.OutputStream).ConfigureAwait(false);
                    }
                    else
                    {
                        // Set the response
                        response.StatusCode = (int)DavStatusCode.NoContent;
                    }
                }
            }

            // Request is handled
            response.Close();
            return true;
        }
    }
}
