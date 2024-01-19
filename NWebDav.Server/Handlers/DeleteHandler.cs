using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers;

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
    private readonly IXmlReaderWriter _xmlReaderWriter;
    private readonly IStore _store;
    private readonly ILockingManager _lockingManager;

    public DeleteHandler(IXmlReaderWriter xmlReaderWriter, IStore store, ILockingManager lockingManager)
    {
        _xmlReaderWriter = xmlReaderWriter;
        _store = store;
        _lockingManager = lockingManager;
    }
    
    /// <summary>
    /// Handle a DELETE request.
    /// </summary>
    /// <param name="httpContext">
    /// The HTTP context of the request.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous DELETE operation. The task
    /// will always return <see langword="true"/> upon completion.
    /// </returns>
    public async Task<bool> HandleRequestAsync(HttpContext httpContext)
    {
        // Obtain request and response
        var request = httpContext.Request;
        var response = httpContext.Response;

        // Keep track of all errors
        var errors = new UriResultCollection();

        // We should always remove the item from a parent container
        var splitUri = RequestHelper.SplitUri(request.GetUri());

        // Obtain parent collection
        var parentCollection = await _store.GetCollectionAsync(splitUri.CollectionUri, httpContext.RequestAborted).ConfigureAwait(false);
        if (parentCollection == null)
        {
            // Source not found
            response.SetStatus(DavStatusCode.NotFound);
            return true;
        }

        // Obtain the item that actually is deleted
        var deleteItem = await parentCollection.GetItemAsync(splitUri.Name, httpContext.RequestAborted).ConfigureAwait(false);
        if (deleteItem == null)
        {
            // Source not found
            response.SetStatus(DavStatusCode.NotFound);
            return true;
        }

        // Check if the item is locked
        if (await _lockingManager.IsLockedAsync(deleteItem, httpContext.RequestAborted).ConfigureAwait(false))
        {
            // Obtain the lock token
            var ifToken = request.GetIfLockToken();
            if (!await _lockingManager.HasLockAsync(deleteItem, ifToken, httpContext.RequestAborted))
            {
                response.SetStatus(DavStatusCode.Locked);
                return true;
            }

            // Remove the token
            await _lockingManager.UnlockAsync(deleteItem, ifToken, httpContext.RequestAborted).ConfigureAwait(false);
        }

        // Delete item
        var status = await DeleteItemAsync(parentCollection, splitUri.Name, splitUri.CollectionUri, httpContext.RequestAborted).ConfigureAwait(false);
        if (status == DavStatusCode.Ok && errors.HasItems)
        {
            // Obtain the status document
            var xDocument = new XDocument(errors.GetXmlMultiStatus());

            // Stream the document
            await _xmlReaderWriter.SendResponseAsync(response, DavStatusCode.MultiStatus, xDocument).ConfigureAwait(false);
        }
        else
        {
            // Return the proper status
            response.SetStatus(status);
        }


        return true;
    }

    private async Task<DavStatusCode> DeleteItemAsync(IStoreCollection collection, string name, Uri baseUri, CancellationToken cancellationToken)
    {
        // Obtain the actual item
        var deleteItem = await collection.GetItemAsync(name, cancellationToken).ConfigureAwait(false);
        if (deleteItem is IStoreCollection deleteCollection)
        {
            // Determine the new base URI
            var subBaseUri = UriHelper.Combine(baseUri, name);

            // Delete all entries first
            await foreach (var entry in deleteCollection.GetItemsAsync(cancellationToken).ConfigureAwait(false))
                await DeleteItemAsync(deleteCollection, entry.Name, subBaseUri, cancellationToken).ConfigureAwait(false);
        }

        // Attempt to delete the item
        return await collection.DeleteItemAsync(name, cancellationToken).ConfigureAwait(false);
    }
}
