using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    [Verb("GET")]
    [Verb("HEAD")]
    public class GetAndHeadHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(IHttpContext httpContext, IStore store)
        {
            // Obtain request and response
            var request = httpContext.Request;
            var response = httpContext.Response;
            var principal = httpContext.Session?.Principal;

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
                    response.SetHeaderValue("Last-Modified", lastModifiedUtc);

                // Add ETag
                var etag = (string)propertyManager.GetProperty(entry, WebDavNamespaces.DavNs + "getetag", true);
                if (etag != null)
                    response.SetHeaderValue("Etag", etag);

                // Add type
                var contentType = (string)propertyManager.GetProperty(entry, WebDavNamespaces.DavNs + "getcontenttype", true);
                if (contentType != null)
                    response.SetHeaderValue("Content-Type", contentType);

                // Add language
                var contentLanguage = (string)propertyManager.GetProperty(entry, WebDavNamespaces.DavNs + "getcontentlanguage", true);
                if (contentLanguage != null)
                    response.SetHeaderValue("Content-Language", contentLanguage);
            }

            // HEAD method doesn't require the actual item data
            if (!head)
            {
                // Stream the actual entry
                using (var stream = entry.GetReadableStream(principal))
                {
                    if (stream != null)
                    {
                        // Set the response
                        response.SendResponse(DavStatusCode.OK);

                        // Set the expected content length
                        try
                        {
                            if (stream.CanSeek)
                                response.SetHeaderValue("Content-Length", stream.Length.ToString(CultureInfo.InvariantCulture));
                        }
                        catch (NotSupportedException)
                        {
                            // If the content length is not supported, then we just skip it
                        }

                        // Copy the entire item
                        await stream.CopyToAsync(response.Stream).ConfigureAwait(false);
                    }
                    else
                    {
                        // Set the response
                        response.SendResponse(DavStatusCode.NoContent);
                    }
                }
            }
            else
            {
                // Set the response for HEAD responses
                response.SendResponse(DavStatusCode.OK);
            }
            return true;
        }
    }
}
