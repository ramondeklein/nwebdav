//using System;
//using System.Net;
//using System.Security.Principal;
//using System.Threading.Tasks;
//using System.Xml.Linq;
//using NWebDav.Server.Helpers;

//namespace NWebDav.Server.Handlers
//{
//    [Verb("MOVE")]
//    public class MoveHandler : IRequestHandler
//    {
//        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStoreResolver storeResolver)
//        {
//            // Obtain request and response
//            var request = httpListenerContext.Request;
//            var response = httpListenerContext.Response;
//            var principal = httpListenerContext.User;

//            // Obtain the destination
//            var destinationUri = request.GetDestinationUri();
//            if (destinationUri == null)
//            {
//                // Bad request
//                response.SendResponse(DavStatusCode.BadRequest, "Destination header is missing.");
//                return true;
//            }

//            // Make sure the source and destination are different
//            if (request.Url.AbsoluteUri.Equals(destinationUri.AbsoluteUri, StringComparison.InvariantCultureIgnoreCase))
//            {
//                // Forbidden
//                response.SendResponse(DavStatusCode.Forbidden, "Source and destination cannot be the same.");
//                return true;
//            }

//            // Check if the Overwrite header is set
//            var overwrite = request.GetOverwrite();

//            // Split the destination Uri
//            var destination = RequestHelper.SplitUri(destinationUri);

//            // Obtain the destination collection
//            var destinationCollection = await storeResolver.GetCollectionAsync(destination.CollectionUri, principal).ConfigureAwait(false);
//            if (destinationCollection == null)
//            {
//                // Source not found
//                response.SendResponse(DavStatusCode.Conflict, "Destination cannot be found.");
//                return true;
//            }

//            // Keep track of all errors
//            var errors = new UriResultCollection();

//            // Obtain the entry as a collection
//            var collection = await storeResolver.GetCollectionAsync(request.Url, principal).ConfigureAwait(false);
//            if (collection == null)
//            {
//                // Fetch the item
//                var item = await storeResolver.GetItemAsync(request.Url, principal).ConfigureAwait(false);
//                if (item == null)
//                {
//                    // Source not found
//                    response.SendResponse(DavStatusCode.NotFound, "Source cannot be found.");
//                    return true;
//                }

//                // Move item
//                await MoveItemAsync(item, destinationCollection, destination.Name, overwrite, principal, request.Url, errors).ConfigureAwait(false);
//            }
//            else
//            {
//                // Move collection
//                await MoveCollectionAsync(collection, destinationCollection, destination.Name, overwrite, principal, request.Url, errors).ConfigureAwait(false);
//            }

//            // Check if there are any errors
//            if (errors.HasItems)
//            {
//                // Obtain the status document
//                var xDocument = new XDocument(errors.GetXmlMultiStatus());

//                // Stream the document
//                await response.SendResponseAsync(DavStatusCode.MultiStatus, xDocument).ConfigureAwait(false);
//            }
//            else
//            {
//                // Set the response
//                response.SendResponse(DavStatusCode.OK);
//            }

//            return true;
//        }

//        private async Task MoveItemAsync(IStoreItem sourceItem, IStoreCollection destinationCollection, string name, bool overwrite, IPrincipal principal, Uri baseUri, UriResultCollection errors)
//        {
//            //// Copy the item
//            //var storeResult = await sourceItem.MoveToAsync(destinationCollection, name, overwrite, principal).ConfigureAwait(false);

//            //// Make sure the item can be copied
//            //if (storeResult != DavStatusCode.Created && storeResult != DavStatusCode.NoContent)
//            //    errors.AddResult(new Uri(baseUri, name), storeResult);
//        }

//        private async Task MoveCollectionAsync(IStoreCollection sourceCollection, IStoreCollection destinationCollection, string name, bool overwrite, IPrincipal principal, Uri baseUri, UriResultCollection errors)
//        {
//            //// Determine the new base Uri
//            //var newBaseUri = new Uri(baseUri, name);

//            //// Copy the collection itself
//            //var newCollectionResult = await sourceCollection.MoveToAsync(destinationCollection, name, overwrite, principal).ConfigureAwait(false);
//            //if (newCollectionResult.Result != DavStatusCode.Created && newCollectionResult.Result != DavStatusCode.NoContent)
//            //{
//            //    errors.AddResult(newBaseUri, newCollectionResult.Result);
//            //}
//        }
//    }
//}
