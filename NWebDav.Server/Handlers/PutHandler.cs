using System;
using System.IO;
using System.Threading.Tasks;

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

            // It's not a collection, so we'll try again by fetching the item in the parent collection
            var splitUri = RequestHelper.SplitUri(request.Url);

            // Obtain collection
            var collection = await store.GetCollectionAsync(splitUri.CollectionUri, httpContext).ConfigureAwait(false);
            if (collection == null)
            {
                // Source not found
                response.SendResponse(DavStatusCode.Conflict);
                return true;
            }

            // Obtain the item
            var result = await collection.CreateItemAsync(splitUri.Name, true, httpContext).ConfigureAwait(false);
            var status = result.Result;
            if (status == DavStatusCode.Created || status == DavStatusCode.NoContent)
            {
                // Upload the information to the item
                var uploadStatus = await result.Item.UploadFromStreamAsync(httpContext, request.Stream).ConfigureAwait(false);
                if (uploadStatus != DavStatusCode.Ok)
                    status = uploadStatus;
            }

            // Finished writing
            response.SendResponse(status);
            return true;
        }
    }
}
