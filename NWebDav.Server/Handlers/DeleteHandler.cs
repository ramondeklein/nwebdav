using System;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    [Verb("DELETE")]
    public class DeleteHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(IHttpContext httpContext, IStore store)
        {
            // Obtain request and response
            var request = httpContext.Request;
            var response = httpContext.Response;
            var principal = httpContext.Session?.Principal;

            // Keep track of all errors
            var errors = new UriResultCollection();

            // We should always remove the item from a parent container
            var splitUri = RequestHelper.SplitUri(request.Url);

            // Obtain parent collection
            var parentCollection = await store.GetCollectionAsync(splitUri.CollectionUri, principal).ConfigureAwait(false);
            if (parentCollection == null)
            {
                // Source not found
                response.SendResponse(DavStatusCode.NotFound);
                return true;
            }

            // Delete item
            await DeleteItemAsync(parentCollection, splitUri.Name, principal, splitUri.CollectionUri, errors).ConfigureAwait(false);

            // Check if there are any errors
            if (errors.HasItems)
            {
                // Obtain the status document
                var xDocument = new XDocument(errors.GetXmlMultiStatus());

                // Stream the document
                await response.SendResponseAsync(DavStatusCode.MultiStatus, xDocument).ConfigureAwait(false);
            }
            else
            {
                // Set the response
                response.SendResponse(DavStatusCode.Ok);
            }
            return true;
        }

        private async Task DeleteItemAsync(IStoreCollection collection, string name, IPrincipal principal, Uri baseUri, UriResultCollection errors)
        {
            // Obtain the actual item
            var deleteCollection = await collection.GetItemAsync(name, principal).ConfigureAwait(false) as IStoreCollection;
            if (deleteCollection != null)
            {
                // Determine the new base URI
                var subBaseUri = UriHelper.Combine(baseUri, name);

                // Delete all entries first
                foreach (var entry in await deleteCollection.GetItemsAsync(principal).ConfigureAwait(false))
                    await DeleteItemAsync(deleteCollection, entry.Name, principal, subBaseUri, errors);
            }

            // Attempt to delete the item
            var storeResult = await collection.DeleteItemAsync(name, principal).ConfigureAwait(false);
            if (storeResult != DavStatusCode.Ok)
                errors.AddResult(UriHelper.Combine(baseUri, name), storeResult);
        }
    }
}
