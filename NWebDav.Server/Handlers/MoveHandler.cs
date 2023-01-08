using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the MOVE method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV MOVE method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_MOVE">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public class MoveHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a MOVE request.
        /// </summary>
        /// <inheritdoc/>
        public async Task<bool> HandleRequestAsync(IHttpContext context, IStore store, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;
            
            // We should always move the item from a parent container
            var splitSourceUri = RequestHelper.SplitUri(request.Url);

            // Obtain source collection
            var sourceCollection = await store.GetCollectionAsync(splitSourceUri.CollectionUri, context).ConfigureAwait(false);
            if (sourceCollection == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound);
                return true;
            }

            // Obtain the item to move
            var moveItem = await sourceCollection.GetItemAsync(splitSourceUri.Name, context).ConfigureAwait(false);
            if (moveItem == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound);
                return true;
            }

            // Obtain the destination
            var destinationUri = request.GetDestinationUri();
            if (destinationUri == null)
            {
                // Bad request
                response.SetStatus(HttpStatusCode.BadRequest, "Destination header is missing.");
                return true;
            }

            // Make sure the source and destination are different
            if (request.Url.AbsoluteUri.Equals(destinationUri.AbsoluteUri, StringComparison.CurrentCultureIgnoreCase))
            {
                // Forbidden
                response.SetStatus(HttpStatusCode.Forbidden, "Source and destination cannot be the same.");
                return true;
            }

            // We should always move the item to a parent
            var splitDestinationUri = RequestHelper.SplitUri(destinationUri);

            // Obtain destination collection
            var destinationCollection = await store.GetCollectionAsync(splitDestinationUri.CollectionUri, context).ConfigureAwait(false);
            if (destinationCollection == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound);
                return true;
            }

            // Check if the Overwrite header is set
            var overwrite = request.GetOverwrite();
            if (!overwrite)
            {
                // If overwrite is false and destination exist ==> Precondition Failed
                var destItem = await destinationCollection.GetItemAsync(splitDestinationUri.Name, context).ConfigureAwait(false);
                if (destItem != null)
                {
                    // Cannot overwrite destination item
                    response.SetStatus(HttpStatusCode.PreconditionFailed, "Cannot overwrite destination item.");
                    return true;
                }
            }

            // Keep track of all errors
            var errors = new UriResultCollection();

            // Move collection
            await MoveAsync(sourceCollection, moveItem, destinationCollection, splitDestinationUri.Name, overwrite, context, splitDestinationUri.CollectionUri, errors).ConfigureAwait(false);

            // Check if there are any errors
            if (errors.HasItems)
            {
                // Obtain the status document
                var xDocument = new XDocument(errors.GetXmlMultiStatus());

                // Stream the document
                await response.SendResponseAsync(HttpStatusCode.MultiStatus, xDocument).ConfigureAwait(false);
            }
            else
            {
                // Set the response
                response.SetStatus(HttpStatusCode.OK);
            }

            return true;
        }

        private async Task MoveAsync(IStoreCollection sourceCollection, IStoreItem moveItem, IStoreCollection destinationCollection, string destinationName, bool overwrite, IHttpContext context, Uri baseUri, UriResultCollection errors)
        {
            // Determine the new base URI
            var subBaseUri = UriHelper.Combine(baseUri, destinationName);

            // Obtain the actual item
            if (moveItem is IStoreCollection moveCollection && !moveCollection.SupportsFastMove(destinationCollection, destinationName, overwrite, context))
            {
                // Create a new collection
                var newCollectionResult = await destinationCollection.CreateCollectionAsync(destinationName, overwrite, context).ConfigureAwait(false);
                if (newCollectionResult.Result != HttpStatusCode.Created && newCollectionResult.Result != HttpStatusCode.NoContent)
                {
                    errors.AddResult(subBaseUri, newCollectionResult.Result);
                    return;
                }

                // Move all sub items
                foreach (var entry in await moveCollection.GetItemsAsync(context).ConfigureAwait(false))
                    await MoveAsync(moveCollection, entry, newCollectionResult.Collection, entry.Name, overwrite, context, subBaseUri, errors).ConfigureAwait(false);

                // Delete the source collection
                var deleteResult = await sourceCollection.DeleteItemAsync(moveItem.Name, context).ConfigureAwait(false);
                if (deleteResult != HttpStatusCode.OK)
                    errors.AddResult(subBaseUri, newCollectionResult.Result);
            }
            else
            {
                // Items should be moved directly
                var result = await sourceCollection.MoveItemAsync(moveItem.Name, destinationCollection, destinationName, overwrite, context).ConfigureAwait(false);
                if (result.Result != HttpStatusCode.Created && result.Result != HttpStatusCode.NoContent)
                    errors.AddResult(subBaseUri, result.Result);
            }
        }
    }
}
