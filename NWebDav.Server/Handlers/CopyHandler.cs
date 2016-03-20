using System;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Helpers;

namespace NWebDav.Server.Handlers
{
    [Verb("COPY")]
    public class CopyHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStoreResolver storeResolver)
        {
            // Obtain request and response
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var principal = httpListenerContext.User;

            // Obtain the destination
            var destinationUri = request.GetDestinationUri();
            if (destinationUri == null)
            {
                // Bad request
                response.SendResponse(DavStatusCode.BadRequest, "Destination header is missing.");
                return true;
            }

            // Make sure the source and destination are different
            if (request.Url.AbsoluteUri.Equals(destinationUri.AbsoluteUri, StringComparison.InvariantCultureIgnoreCase))
            {
                // Forbidden
                response.SendResponse(DavStatusCode.Forbidden, "Source and destination cannot be the same.");
                return true;
            }

            // Check if the Overwrite header is set
            var overwrite = request.GetOverwrite();

            // Split the destination Uri
            var destination = RequestHelper.SplitUri(destinationUri);

            // Obtain the destination collection
            var destinationCollection = await storeResolver.GetCollectionAsync(destination.CollectionUri, principal);
            if (destinationCollection == null)
            {
                // Source not found
                response.SendResponse(DavStatusCode.Conflict, "Destination cannot be found.");
                return true;
            }

            // Keep track of all errors
            var errors = new UriResultCollection();

            // Obtain the source item
            var item = await storeResolver.GetItemAsync(request.Url, principal);
            if (item == null)
            {
                // Obtain collection
                var collection = await storeResolver.GetCollectionAsync(request.Url, principal);
                if (collection == null)
                {
                    // Source not found
                    response.SendResponse(DavStatusCode.NotFound, "Source cannot be found.");
                    return true;
                }

                // Determine depth
                var depth = request.GetDepth();

                // Copy collection
                await CopyCollectionAsync(collection, destinationCollection, destination.Name, overwrite, depth, principal, destination.CollectionUri, errors);
            }
            else
            {
                // Copy item
                await CopyItemAsync(item, destinationCollection, destination.Name, overwrite, principal, destination.CollectionUri, errors);
            }

            // Check if there are any errors
            if (errors.HasItems)
            {
                // Obtain the status document
                var xDocument = new XDocument(errors.GetXmlMultiStatus());

                // Stream the document
                await response.SendResponseAsync(DavStatusCode.MultiStatus, xDocument);
            }
            else
            {
                // Set the response
                response.SendResponse(DavStatusCode.OK);
            }

            return true;
        }

        private async Task CopyItemAsync(IStoreItem sourceItem, IStoreCollection destinationCollection, string name, bool overwrite, IPrincipal principal, Uri baseUri, UriResultCollection errors)
        {
            // Copy the item
            var storeResult = await sourceItem.CopyToAsync(destinationCollection, name, overwrite, principal);

            // Make sure the item can be copied
            if (storeResult != DavStatusCode.Created && storeResult != DavStatusCode.NoContent)
                errors.AddResult(new Uri(baseUri, name), storeResult);
        }

        private async Task CopyCollectionAsync(IStoreCollection sourceCollection, IStoreCollection destinationCollection, string name, bool overwrite, int depth, IPrincipal principal, Uri baseUri, UriResultCollection errors)
        {
            // Determine the new base Uri
            var newBaseUri = new Uri(baseUri, name);

            // Copy the collection itself
            var newCollectionResult = await sourceCollection.CopyToAsync(destinationCollection, name, overwrite, principal);
            if (newCollectionResult.Result != DavStatusCode.Created && newCollectionResult.Result != DavStatusCode.NoContent)
            {
                errors.AddResult(newBaseUri, newCollectionResult.Result);
            }
            else if (depth > 0)
            {
                // If the depth is set, then the content needs to be copied too
                foreach (var entry in await sourceCollection.GetEntriesAsync(principal))
                {
                    var collection = entry as IStoreCollection;
                    if (collection != null)
                    {
                        // Copy collection
                        await CopyCollectionAsync(collection, newCollectionResult.Collection, collection.Name, overwrite, depth-1, principal, newBaseUri, errors);
                    }
                    else
                    {
                        // Copy item
                        var item = (IStoreItem)entry;
                        await CopyItemAsync(item, newCollectionResult.Collection, item.Name, overwrite, principal, newBaseUri, errors);
                    }
                }
            }
        }
    }
}
