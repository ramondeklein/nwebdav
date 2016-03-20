using System;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Helpers;

namespace NWebDav.Server.Handlers
{
    [Verb("DELETE")]
    public class DeleteHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStoreResolver storeResolver)
        {
            // Obtain request and response
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var principal = httpListenerContext.User;

            // Keep track of all errors
            var errors = new UriResultCollection();

            // Obtain the entry as a collection
            var collection = await storeResolver.GetCollectionAsync(request.Url, principal).ConfigureAwait(false);
            if (collection == null)
            {
                // It's not a collection, so we'll try again by fetching the item in the parent collection
                var splitUri = RequestHelper.SplitUri(request.Url);

                // Obtain collection
                collection = await storeResolver.GetCollectionAsync(splitUri.CollectionUri, principal).ConfigureAwait(false);
                if (collection == null)
                {
                    // Source not found
                    response.SendResponse(DavStatusCode.NotFound);
                    return true;
                }

                // Delete item
                await DeleteItemAsync(collection, splitUri.Name, principal, splitUri.CollectionUri, errors).ConfigureAwait(false);
            }
            else
            {
                // Delete collection
                await DeleteCollectionAsync(collection, principal, request.Url, errors).ConfigureAwait(false);
            }

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
                response.SendResponse(DavStatusCode.OK);
            }
            return true;
        }

        private async Task DeleteItemAsync(IStoreCollection collection, string name, IPrincipal principal, Uri baseUri, UriResultCollection errors)
        {
            // Attempt to delete the item
            var storeResult = await collection.DeleteItemAsync(name, principal).ConfigureAwait(false);
            if (storeResult != DavStatusCode.OK)
                errors.AddResult(new Uri(baseUri, name), storeResult);
        }

        private async Task DeleteCollectionAsync(IStoreCollection collection, IPrincipal principal, Uri baseUri, UriResultCollection errors)
        {
            // Delete all entries first
            foreach (var entry in await collection.GetEntriesAsync(principal).ConfigureAwait(false))
            {
                var subCollection = entry as IStoreCollection;
                if (subCollection != null)
                {
                    // Delete sub-collection
                    await DeleteCollectionAsync(subCollection, principal, new Uri(baseUri, entry.Name), errors).ConfigureAwait(false);
                }
                else
                {
                    var item = (IStoreItem)entry;
                    await DeleteItemAsync(collection, item.Name, principal, baseUri, errors).ConfigureAwait(false);
                }
            }

            // Delete the collection itself
            var storeResult = await collection.DeleteAsync(principal).ConfigureAwait(false);
            if (storeResult != DavStatusCode.OK)
                errors.AddResult(baseUri, storeResult);
        }
    }
}
