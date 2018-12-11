using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the DELETE method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV DELETE method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_DELETE">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public class DeleteHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a DELETE request.
        /// </summary>
        /// <param name="httpContext">
        /// The HTTP context of the request.
        /// </param>
        /// <param name="store">
        /// Store that is used to access the collections and items.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous DELETE operation. The task
        /// will always return <see langword="true"/> upon completion.
        /// </returns>
        public async Task<bool> HandleRequestAsync(IHttpContext httpContext, IStore store, CancellationToken cancellationToken)
        {
            // Obtain request and response
            var request = httpContext.Request;
            var response = httpContext.Response;

            // Keep track of all errors
            var errors = new UriResultCollection();

            // We should always remove the item from a parent container
            var splitUri = RequestHelper.SplitUri(request.Url);

            // Obtain parent collection
            var parentCollection = await store.GetCollectionAsync(splitUri.CollectionUri, httpContext, cancellationToken).ConfigureAwait(false);
            if (parentCollection == null)
            {
                // Source not found
                response.SetStatus(DavStatusCode.NotFound);
                return true;
            }

            // Obtain the item that actually is deleted
            var deleteItem = await parentCollection.GetItemAsync(splitUri.Name, httpContext, cancellationToken).ConfigureAwait(false);
            if (deleteItem == null)
            {
                // Source not found
                response.SetStatus(DavStatusCode.NotFound);
                return true;
            }

            // Check if the item is locked
            var isLocked = false;
            if (deleteItem.LockingManager != null)
                isLocked = await deleteItem.LockingManager.IsLockedAsync(deleteItem, cancellationToken).ConfigureAwait(false);

            if (isLocked)
            {
                // Obtain the lock token
                var ifToken = request.GetIfLockToken();
                var hasLock = await deleteItem.LockingManager.HasLockAsync(deleteItem, ifToken, cancellationToken).ConfigureAwait(false);
                if (!hasLock)
                {
                    response.SetStatus(DavStatusCode.Locked);
                    return true;
                }

                // Remove the token
                await deleteItem.LockingManager.UnlockAsync(deleteItem, ifToken, cancellationToken);
            }

            // Delete item
            var status = await DeleteItemAsync(parentCollection, splitUri.Name, httpContext, splitUri.CollectionUri, cancellationToken).ConfigureAwait(false);
            if (status == DavStatusCode.Ok && errors.HasItems)
            {
                // Obtain the status document
                var xDocument = new XDocument(errors.GetXmlMultiStatus());

                // Stream the document
                await response.SendResponseAsync(DavStatusCode.MultiStatus, xDocument, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Return the proper status
                response.SetStatus(status);
            }


            return true;
        }

        private async Task<DavStatusCode> DeleteItemAsync(IStoreCollection collection, string name, IHttpContext httpContext, Uri baseUri, CancellationToken cancellationToken)
        {
            // Obtain the actual item
            var deleteItem = await collection.GetItemAsync(name, httpContext, cancellationToken).ConfigureAwait(false);
            if (deleteItem is IStoreCollection deleteCollection)
            {
                // Determine the new base URI
                var subBaseUri = UriHelper.Combine(baseUri, name);

                // Delete all entries first
                foreach (var entry in await deleteCollection.GetItemsAsync(httpContext, cancellationToken).ConfigureAwait(false))
                    await DeleteItemAsync(deleteCollection, entry.Name, httpContext, subBaseUri, cancellationToken).ConfigureAwait(false);
            }

            // Attempt to delete the item
            return await collection.DeleteItemAsync(name, httpContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
