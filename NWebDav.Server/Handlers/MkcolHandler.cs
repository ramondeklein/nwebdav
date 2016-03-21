using System;
using System.Net;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;

namespace NWebDav.Server.Handlers
{
    [Verb("MKCOL")]
    public class MkcolHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStoreResolver storeResolver)
        {
            // Obtain request and response
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var principal = httpListenerContext.User;

            // The collection must always be created inside another collection
            var splitUri = RequestHelper.SplitUri(request.Url);

            // Obtain the parent entry
            var collection = await storeResolver.GetCollectionAsync(splitUri.CollectionUri, principal).ConfigureAwait(false);
            if (collection == null)
            {
                // Source not found
                response.SendResponse(DavStatusCode.Conflict);
                return true;
            }

            // Create the collection
            var result = await collection.CreateCollectionAsync(splitUri.Name, false, principal).ConfigureAwait(false);

            // Finished
            response.SendResponse(result.Result);
            return true;
        }
    }
}
