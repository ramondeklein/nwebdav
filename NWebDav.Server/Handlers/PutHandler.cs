using System;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    public class PutHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(IHttpContext httpContext, IStore store)
        {
            // Obtain request and response
            var request = httpContext.Request;
            var response = httpContext.Response;
            var principal = httpContext.Session?.Principal;

            // It's not a collection, so we'll try again by fetching the item in the parent collection
            var splitUri = RequestHelper.SplitUri(request.Url);

            // Obtain collection
            var collection = await store.GetCollectionAsync(splitUri.CollectionUri, principal).ConfigureAwait(false);
            if (collection == null)
            {
                // Source not found
                response.SendResponse(DavStatusCode.Conflict);
                return true;
            }

            // Obtain the item
            var result = await collection.CreateItemAsync(splitUri.Name, true, principal).ConfigureAwait(false);
            var status = result.Result;
            if (status == DavStatusCode.Created || status == DavStatusCode.NoContent)
            {
                // Copy the stream
                try
                {
                    using (var destinationStream = result.Item.GetWritableStream(principal))
                    {
                        await request.Stream.CopyToAsync(destinationStream).ConfigureAwait(false);
                    }
                }
                catch (IOException ioException) when (ioException.IsDiskFull())
                {
                    // TODO: Log exceeption
                    status = DavStatusCode.InsufficientStorage;
                }
            }

            // Finished writing
            response.SendResponse(status);
            return true;
        }
    }
}
