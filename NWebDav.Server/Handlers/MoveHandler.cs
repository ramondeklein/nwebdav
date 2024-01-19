using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Helpers;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers;

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
    private readonly IXmlReaderWriter _xmlReaderWriter;
    private readonly IStore _store;

    public MoveHandler(IXmlReaderWriter xmlReaderWriter, IStore store)
    {
        _xmlReaderWriter = xmlReaderWriter;
        _store = store;
    }
    
    /// <summary>
    /// Handle a MOVE request.
    /// </summary>
    /// <param name="httpContext">
    /// The HTTP context of the request.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous MOVE operation. The task
    /// will always return <see langword="true"/> upon completion.
    /// </returns>
    public async Task<bool> HandleRequestAsync(HttpContext httpContext)
    {
        // Obtain request and response
        var request = httpContext.Request;
        var response = httpContext.Response;
        
        // We should always move the item from a parent container
        var splitSourceUri = RequestHelper.SplitUri(request.GetUri());

        // Obtain source collection
        var sourceCollection = await _store.GetCollectionAsync(splitSourceUri.CollectionUri, httpContext.RequestAborted).ConfigureAwait(false);
        if (sourceCollection == null)
        {
            // Source not found
            response.SetStatus(DavStatusCode.NotFound);
            return true;
        }

        // Obtain the item to move
        var moveItem = await sourceCollection.GetItemAsync(splitSourceUri.Name, httpContext.RequestAborted).ConfigureAwait(false);
        if (moveItem == null)
        {
            // Source not found
            response.SetStatus(DavStatusCode.NotFound);
            return true;
        }

        // Obtain the destination
        var destinationUri = request.GetDestinationUri();
        if (destinationUri == null)
        {
            // Bad request
            response.SetStatus(DavStatusCode.BadRequest, "Destination header is missing.");
            return true;
        }

        // Make sure the source and destination are different
        if (request.GetUri().AbsoluteUri.Equals(destinationUri.AbsoluteUri, StringComparison.CurrentCultureIgnoreCase))
        {
            // Forbidden
            response.SetStatus(DavStatusCode.Forbidden, "Source and destination cannot be the same.");
            return true;
        }

        // We should always move the item to a parent
        var splitDestinationUri = RequestHelper.SplitUri(destinationUri);

        // Obtain destination collection
        var destinationCollection = await _store.GetCollectionAsync(splitDestinationUri.CollectionUri, httpContext.RequestAborted).ConfigureAwait(false);
        if (destinationCollection == null)
        {
            // Source not found
            response.SetStatus(DavStatusCode.NotFound);
            return true;
        }

        // Check if the Overwrite header is set
        var overwrite = request.GetOverwrite();
        if (!overwrite)
        {
            // If overwrite is false and destination exist ==> Precondition Failed
            var destItem = await destinationCollection.GetItemAsync(splitDestinationUri.Name, httpContext.RequestAborted).ConfigureAwait(false);
            if (destItem != null)
            {
                // Cannot overwrite destination item
                response.SetStatus(DavStatusCode.PreconditionFailed, "Cannot overwrite destination item.");
                return true;
            }
        }

        // Keep track of all errors
        var errors = new UriResultCollection();

        // Move collection
        await MoveAsync(sourceCollection, moveItem, destinationCollection, splitDestinationUri.Name, overwrite, splitDestinationUri.CollectionUri, errors, httpContext.RequestAborted).ConfigureAwait(false);

        // Check if there are any errors
        if (errors.HasItems)
        {
            // Obtain the status document
            var xDocument = new XDocument(errors.GetXmlMultiStatus());

            // Stream the document
            await _xmlReaderWriter.SendResponseAsync(response, DavStatusCode.MultiStatus, xDocument).ConfigureAwait(false);
        }
        else
        {
            // Set the response
            response.SetStatus(DavStatusCode.Ok);
        }

        return true;
    }

    private async Task MoveAsync(IStoreCollection sourceCollection, IStoreItem moveItem, IStoreCollection destinationCollection, string destinationName, bool overwrite, Uri baseUri, UriResultCollection errors, CancellationToken cancellationToken)
    {
        // Determine the new base URI
        var subBaseUri = UriHelper.Combine(baseUri, destinationName);

        // Obtain the actual item
        if (moveItem is IStoreCollection moveCollection && !moveCollection.SupportsFastMove(destinationCollection, destinationName, overwrite))
        {
            // Create a new collection
            var newCollectionResult = await destinationCollection.CreateCollectionAsync(destinationName, overwrite, cancellationToken).ConfigureAwait(false);
            if (newCollectionResult.Result != DavStatusCode.Created && newCollectionResult.Result != DavStatusCode.NoContent)
            {
                errors.AddResult(subBaseUri, newCollectionResult.Result);
                return;
            }

            // Move all sub items
            await foreach (var entry in moveCollection.GetItemsAsync(cancellationToken).ConfigureAwait(false))
                await MoveAsync(moveCollection, entry, newCollectionResult.Collection, entry.Name, overwrite, subBaseUri, errors, cancellationToken).ConfigureAwait(false);

            // Delete the source collection
            var deleteResult = await sourceCollection.DeleteItemAsync(moveItem.Name, cancellationToken).ConfigureAwait(false);
            if (deleteResult != DavStatusCode.Ok)
                errors.AddResult(subBaseUri, newCollectionResult.Result);
        }
        else
        {
            // Items should be moved directly
            var result = await sourceCollection.MoveItemAsync(moveItem.Name, destinationCollection, destinationName, overwrite, cancellationToken).ConfigureAwait(false);
            if (result.Result != DavStatusCode.Created && result.Result != DavStatusCode.NoContent)
                errors.AddResult(subBaseUri, result.Result);
        }
    }
}
