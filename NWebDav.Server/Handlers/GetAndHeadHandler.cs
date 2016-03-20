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
            var item = await storeResolver.GetItemAsync(request.Url, principal);
            if (item == null)
            {
                // Set status to not found
                response.SendResponse(DavStatusCode.NotFound);
                return true;
            }

            // Set the response
            response.StatusCode = (int)DavStatusCode.OK;

            // Add last modification timestamp
            var lastModifiedUtc = item.LastModifiedUtc;
            if (lastModifiedUtc.HasValue)
                response.AppendHeader("Last-Modified", lastModifiedUtc.Value.ToString("R"));

            // Add ETag
            var etag = item.Etag;
            if (etag != null)
                response.AppendHeader("Etag", etag);

            // Add type
            var contentType = item.ContentType;
            if (contentType != null)
                response.AppendHeader("Content-Type", contentType);

            // Add language
            var contentLanguage = item.ContentLanguage;
            if (contentLanguage != null)
                response.AppendHeader("Content-Language", contentLanguage);

            // HEAD method doesn't require the actual item data
            if (head)
            {
                // Close response
                response.Close();
            }
            else
            {
                // Set the expected content length
                var contentLength = item.ContentLength;
                if (contentLength.HasValue)
                    response.ContentLength64 = contentLength.Value;

                // Stream the actual item
                using (var stream = item.GetReadableStream(principal))
                {
                    await stream.CopyToAsync(response.OutputStream);
                }
            }

            // Request is handled
            return true;
        }
    }
}
